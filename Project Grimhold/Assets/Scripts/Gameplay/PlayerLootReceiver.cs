using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Owns the player's temporary loot collection for the current incursion.
/// State Authority is the only writer, while Fusion snapshots provide a consistent
/// read-only view to the owning player and other peers observing the player object.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerLootReceiver : NetworkBehaviour, ILootReceiver
{
    public const int MaxLootTypes = 64;

    [SerializeField]
    private LootDefinitionCatalog _lootCatalog;

    [Networked, Capacity(MaxLootTypes)]
    private NetworkDictionary<int, int> LootInventory => default;

    [Networked]
    public int LootChangeSequence { get; private set; }

    [Networked]
    private int LastGrantedDefinitionIndex { get; set; }

    [Networked]
    private int LastGrantedAmount { get; set; }

    [Networked]
    private int LastGrantSourceIdValue { get; set; }

    [Networked]
    private int LastGrantTick { get; set; }

    private EntityRegistry _registry;
    private ICharacter _character;
    private bool _isRegistered;
    private EntityId _registeredId;
    private readonly Queue<LootGrantPresentationEvent> _pendingPresentationEvents = new();

    /// <summary>
    /// Local presentation notification emitted during Render on the receiving player's peer.
    /// </summary>
    public event Action<LootGrantPresentationEvent> LootGranted;

    /// <summary>
    /// Gets the number of distinct loot definitions currently held.
    /// </summary>
    public int DistinctLootCount => LootInventory.Count;

    public new EntityId Id
    {
        get
        {
            if (_character != null)
            {
                return _character.Id;
            }
            return default;
        }
    }

    private void Awake()
    {
        _character = GetComponent<ICharacter>();
    }

    public override void Spawned()
    {
        if (!ValidateDependencies())
        {
            return;
        }

        if (!HasStateAuthority)
        {
            return;
        }

        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry != null)
        {
            _registeredId = Id;
            _isRegistered = _registry.TryRegisterLootReceiver(_registeredId, this);
            if (!_isRegistered)
            {
                Debug.LogError($"{nameof(PlayerLootReceiver)}: Failed to register to {nameof(EntityRegistry)} with ID {_registeredId}.", this);
            }
        }
        else
        {
            Debug.LogError($"{nameof(PlayerLootReceiver)}: EntityRegistry was not found on the NetworkRunner.", this);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_isRegistered && _registry != null)
        {
            _registry.TryUnregisterLootReceiver(_registeredId, this);
            _isRegistered = false;
        }

        _pendingPresentationEvents.Clear();
    }

    public override void Render()
    {
        while (_pendingPresentationEvents.Count > 0)
        {
            LootGranted?.Invoke(_pendingPresentationEvents.Dequeue());
        }
    }

    public LootReceiveResult TryGrantLoot(in LootGrantRequest request)
    {
        if (!HasStateAuthority)
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.MissingStateAuthority);
        }

        if (request.ReceiverId != Id)
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.ReceiverUnavailable);
        }

        if (string.IsNullOrWhiteSpace(request.LootId.Value))
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.InvalidLootId);
        }

        if (request.Amount <= 0)
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.InvalidAmount);
        }

        if (_lootCatalog == null || !_lootCatalog.TryGetIndex(request.LootId, out int definitionIndex))
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.UnknownDefinition);
        }

        if (definitionIndex < 0 || definitionIndex >= MaxLootTypes)
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.InvalidNetworkRepresentation);
        }

        NetworkDictionary<int, int> inventory = LootInventory;
        bool alreadyHeld = inventory.TryGet(definitionIndex, out int currentAmount);

        if (!alreadyHeld && inventory.Count >= inventory.Capacity)
        {
            // A valid catalog cannot reach this branch because its size is validated
            // against MaxLootTypes before the receiver is registered.
            return LootReceiveResult.Rejected(LootReceiveFailureReason.InvalidNetworkRepresentation);
        }

        if (currentAmount > int.MaxValue - request.Amount)
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.Overflow);
        }

        inventory.Set(definitionIndex, currentAmount + request.Amount);

        LootChangeSequence++;
        LastGrantedDefinitionIndex = definitionIndex;
        LastGrantedAmount = request.Amount;
        LastGrantSourceIdValue = request.SourceId.Value;
        LastGrantTick = request.SimulationTick;

        RPC_ReceiveLootGrant(
            LootChangeSequence,
            request.SourceId.Value,
            definitionIndex,
            request.Amount,
            request.SimulationTick);

        return LootReceiveResult.Accepted();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ReceiveLootGrant(
        int sequence,
        int sourceIdValue,
        int definitionIndex,
        int amount,
        int simulationTick)
    {
        if (_lootCatalog == null || !_lootCatalog.TryGetByIndex(definitionIndex, out LootDefinition definition))
        {
            Debug.LogError($"{nameof(PlayerLootReceiver)}: Cannot resolve loot grant index {definitionIndex} for local presentation.", this);
            return;
        }

        _pendingPresentationEvents.Enqueue(new LootGrantPresentationEvent(
            sequence,
            new EntityId(sourceIdValue),
            Id,
            definition.LootId,
            amount,
            simulationTick));
    }

    /// <summary>
    /// Gets the aggregated amount held for a loot definition.
    /// </summary>
    public int GetLootAmount(LootId lootId)
    {
        if (_lootCatalog == null || !_lootCatalog.TryGetIndex(lootId, out int definitionIndex))
        {
            return 0;
        }

        return LootInventory.TryGet(definitionIndex, out int amount) ? amount : 0;
    }

    /// <summary>
    /// Creates a stable, immutable snapshot of the currently held loot.
    /// Mutating or replacing the returned entries cannot change networked state.
    /// </summary>
    public IReadOnlyList<LootEntry> GetLootContent()
    {
        if (!TryGetLootContent(out IReadOnlyList<LootEntry> entries))
        {
            throw new InvalidOperationException($"{nameof(PlayerLootReceiver)} cannot resolve the complete loot contents from its catalog.");
        }

        return entries;
    }

    /// <summary>
    /// Attempts to create a complete immutable snapshot of the held loot.
    /// No partial content is returned when catalog resolution fails.
    /// </summary>
    public bool TryGetLootContent(out IReadOnlyList<LootEntry> content)
    {
        content = Array.Empty<LootEntry>();
        if (_lootCatalog == null)
        {
            return false;
        }

        var entries = new List<LootEntry>(LootInventory.Count);
        int definitionCount = _lootCatalog.DefinitionCount;

        for (int index = 0; index < definitionCount; index++)
        {
            if (!LootInventory.TryGet(index, out int amount))
            {
                continue;
            }

            if (!_lootCatalog.TryGetByIndex(index, out LootDefinition definition))
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

    /// <summary>
    /// Calculates the current extraction value from replicated quantities and local definitions.
    /// The total is derived and is never stored as separate mutable network state.
    /// </summary>
    public long CalculateTotalValue()
    {
        if (!TryCalculateTotalValue(out long total))
        {
            throw new InvalidOperationException($"{nameof(PlayerLootReceiver)} cannot calculate a complete value from its catalog.");
        }

        return total;
    }

    /// <summary>
    /// Attempts to calculate the complete derived extraction value.
    /// The output remains zero when any catalog entry or arithmetic operation is invalid.
    /// </summary>
    public bool TryCalculateTotalValue(out long total)
    {
        total = 0;
        if (_lootCatalog == null)
        {
            return false;
        }

        try
        {
            foreach (KeyValuePair<int, int> pair in LootInventory)
            {
                if (!_lootCatalog.TryGetByIndex(pair.Key, out LootDefinition definition))
                {
                    total = 0;
                    return false;
                }

                total = checked(total + checked((long)pair.Value * definition.ExtractionValuePerUnit));
            }
        }
        catch (OverflowException)
        {
            total = 0;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves immutable visual metadata for a loot identifier from the assigned catalog.
    /// </summary>
    public bool TryResolveDefinition(LootId lootId, out LootDefinition definition)
    {
        definition = null;
        return _lootCatalog != null && _lootCatalog.TryGet(lootId.Value, out definition);
    }

    private bool ValidateDependencies()
    {
        if (_character == null)
        {
            Debug.LogError($"{nameof(PlayerLootReceiver)}: No component implementing {nameof(ICharacter)} is found on {gameObject.name}.", this);
            return false;
        }

        if (_lootCatalog == null)
        {
            Debug.LogError($"{nameof(PlayerLootReceiver)}: Loot catalog is not assigned on {gameObject.name}.", this);
            return false;
        }

        if (!_lootCatalog.TryValidate(out string catalogError))
        {
            Debug.LogError($"{nameof(PlayerLootReceiver)}: Loot catalog is invalid. {catalogError}", this);
            return false;
        }

        if (_lootCatalog.DefinitionCount > MaxLootTypes)
        {
            Debug.LogError($"{nameof(PlayerLootReceiver)}: Loot catalog contains {_lootCatalog.DefinitionCount} definitions, exceeding the network representation limit of {MaxLootTypes}.", this);
            return false;
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_lootCatalog != null)
        {
            return;
        }

        string[] catalogGuids = UnityEditor.AssetDatabase.FindAssets("t:LootDefinitionCatalog");
        if (catalogGuids.Length != 1)
        {
            return;
        }

        string catalogPath = UnityEditor.AssetDatabase.GUIDToAssetPath(catalogGuids[0]);
        _lootCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<LootDefinitionCatalog>(catalogPath);
    }
#endif
}
