using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Authoritative reusable loot source whose stack contents and availability are replicated by Fusion.
/// It exposes extraction and read capabilities without knowing players, interaction UI or receiver types.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkLootContainer : NetworkBehaviour,
    ILootExtractor,
    ILootQuantityReader,
    ILootContentReader,
    ILootSlotCapacityReader
{
    public const int MaxLootTypes = 64;

    [SerializeField]
    private LootDefinitionCatalog _lootCatalog;

    [SerializeField, Range(1, MaxLootTypes)]
    private int _slotCapacity = 16;

    [SerializeField]
    private bool _startsAvailable = true;

    [SerializeField]
    private LootContainerInitialEntry[] _initialContent = Array.Empty<LootContainerInitialEntry>();

    [Networked, Capacity(MaxLootTypes)]
    private NetworkDictionary<int, int> LootInventory => default;

    [Networked]
    public NetworkBool IsInitialized { get; private set; }

    [Networked]
    public NetworkBool IsAvailable { get; private set; }

    [Networked]
    public int LootChangeSequence { get; private set; }

    private Collider2D[] _cachedColliders;
    private EntityRegistry _registry;
    private EntityId _registeredId;
    private bool _isRegistered;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool _hasQueuedDebugAvailability;
    private bool _queuedDebugAvailability;
#endif

    public new EntityId Id => Object != null ? new EntityId(unchecked((int)Object.Id.Raw)) : default;
    public int SlotCapacity => _slotCapacity;
    public int OccupiedSlotCount => LootInventory.Count;
    public bool IsEmpty => LootInventory.Count == 0;

    private void Awake()
    {
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);
    }

    public override void Spawned()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (!LootContainerInitializationRules.TryBuild(
                _initialContent,
                _lootCatalog,
                _slotCapacity,
                MaxLootTypes,
                out IReadOnlyList<KeyValuePair<int, int>> resolvedEntries,
                out string error))
        {
            Debug.LogError($"{nameof(NetworkLootContainer)}: Invalid initial configuration on {name}. {error}", this);
            return;
        }

        NetworkDictionary<int, int> inventory = LootInventory;
        for (int i = 0; i < resolvedEntries.Count; i++)
        {
            KeyValuePair<int, int> entry = resolvedEntries[i];
            inventory.Set(entry.Key, entry.Value);
        }

        IsInitialized = true;
        IsAvailable = false;

        _registry = Runner.GetComponent<EntityRegistry>();
        _registeredId = Id;
        if (_registry == null || !_registry.TryRegisterLootSource(_registeredId, this, this, _cachedColliders))
        {
            Debug.LogError(
                $"{nameof(NetworkLootContainer)}: Failed to register initialized container '{name}' with ID {_registeredId}. Contents were preserved and the source remains unavailable.",
                this);
            return;
        }

        _isRegistered = true;
        IsAvailable = _startsAvailable;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_isRegistered && _registry != null)
        {
            _registry.TryUnregisterLootSource(_registeredId, this, this);
        }

        _isRegistered = false;
        _registry = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        _hasQueuedDebugAvailability = false;
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !_hasQueuedDebugAvailability)
        {
            return;
        }

        bool availability = _queuedDebugAvailability;
        _hasQueuedDebugAvailability = false;
        SetAvailability(availability);
        Debug.Log($"{nameof(NetworkLootContainer)}: Debug availability changed to {availability} for '{name}'.", this);
    }

    /// <summary>
    /// Queues a development-only availability change for the next authoritative simulation tick.
    /// </summary>
    public bool DebugTryQueueAvailability(bool isAvailable)
    {
        if (!HasStateAuthority)
        {
            return false;
        }

        _queuedDebugAvailability = isAvailable;
        _hasQueuedDebugAvailability = true;
        return true;
    }
#endif

    /// <summary>
    /// Changes runtime availability on State Authority without changing contents, registration or change sequence.
    /// Enabling requires completed initialization and a successful grouped registry registration.
    /// </summary>
    public void SetAvailability(bool isAvailable)
    {
        if (!HasStateAuthority)
        {
            throw new InvalidOperationException($"{nameof(SetAvailability)} requires State Authority.");
        }

        if (Runner == null || !Runner.IsSimulationUpdating)
        {
            throw new InvalidOperationException($"{nameof(SetAvailability)} must be called from authoritative simulation flow.");
        }

        if (isAvailable && (!IsInitialized || !_isRegistered))
        {
            throw new InvalidOperationException("An uninitialized or unregistered loot container cannot be made available.");
        }

        if (IsAvailable == isAvailable)
        {
            return;
        }

        IsAvailable = isAvailable;
    }

    public LootTransferFailureReason ValidateExtraction(in LootTransferRequest request)
    {
        if (!HasStateAuthority)
        {
            return LootTransferFailureReason.MissingAuthority;
        }

        if (!IsInitialized || !IsAvailable || !_isRegistered)
        {
            return LootTransferFailureReason.ContainerUnavailable;
        }

        if (request.SourceId != Id)
        {
            return LootTransferFailureReason.SourceNotFound;
        }

        if (request.DestinationId.Value == 0)
        {
            return LootTransferFailureReason.DestinationNotFound;
        }

        if (!request.LootId.IsValid || _lootCatalog == null || !_lootCatalog.TryGetIndex(request.LootId, out int index))
        {
            return LootTransferFailureReason.InvalidLoot;
        }

        if (request.RequestedAmount <= 0)
        {
            return LootTransferFailureReason.InvalidAmount;
        }

        bool hasStack = LootInventory.TryGet(index, out int currentAmount);
        return LootInventoryRules.ValidateExtraction(hasStack, currentAmount, request.RequestedAmount);
    }

    public void CommitExtraction(in LootTransferRequest request)
    {
        EnsureCommitContract(request, out int index, out int currentAmount);

        int remainingAmount = checked(currentAmount - request.RequestedAmount);
        if (remainingAmount == 0)
        {
            LootInventory.Remove(index);
        }
        else
        {
            LootInventory.Set(index, remainingAmount);
        }

        LootChangeSequence++;
    }

    public int GetLootAmount(LootId lootId)
    {
        return _lootCatalog != null &&
            _lootCatalog.TryGetIndex(lootId, out int index) &&
            LootInventory.TryGet(index, out int amount) && amount > 0
                ? amount
                : 0;
    }

    public bool TryGetLootContent(out IReadOnlyList<LootEntry> content)
    {
        content = Array.Empty<LootEntry>();
        if (_lootCatalog == null)
        {
            return false;
        }

        var entries = new List<LootEntry>(LootInventory.Count);
        for (int index = 0; index < _lootCatalog.DefinitionCount; index++)
        {
            if (!LootInventory.TryGet(index, out int amount))
            {
                continue;
            }

            if (amount <= 0 || !_lootCatalog.TryGetByIndex(index, out LootDefinition definition))
            {
                return false;
            }

            entries.Add(new LootEntry(definition.LootId, amount));
        }

        if (entries.Count != LootInventory.Count)
        {
            return false;
        }

        content = entries.AsReadOnly();
        return true;
    }

    private void EnsureCommitContract(
        in LootTransferRequest request,
        out int index,
        out int currentAmount)
    {
        if (!HasStateAuthority || !IsInitialized || !IsAvailable || !_isRegistered ||
            request.SourceId != Id || request.DestinationId.Value == 0 ||
            request.RequestedAmount <= 0 || _lootCatalog == null ||
            !_lootCatalog.TryGetIndex(request.LootId, out index) ||
            !LootInventory.TryGet(index, out currentAmount) || currentAmount < request.RequestedAmount)
        {
            Debug.LogError($"{nameof(NetworkLootContainer)}: {nameof(CommitExtraction)} contract was violated for '{name}'.", this);
            throw new InvalidOperationException("Loot extraction commit preconditions changed after successful validation.");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _slotCapacity = Mathf.Clamp(_slotCapacity, 1, MaxLootTypes);
    }
#endif
}
