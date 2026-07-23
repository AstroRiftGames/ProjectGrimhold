using Fusion;
using UnityEngine;

/// <summary>
/// Synchronized network component that manages world loot pickup.
/// Implements IPickup and interacts only under State Authority.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkLootPickup : NetworkBehaviour, IPickup
{
    [Header("Loot Configuration")]
    [SerializeField]
    private LootDefinition _lootDefinition;

    [SerializeField]
    private int _amount = 1;

    [SerializeField]
    private LootDefinitionCatalog _lootCatalog;

    [SerializeField]
    private SpriteRenderer _worldRenderer;

    [Header("World Presentation")]
    [SerializeField]
    private string _sortingLayerName = "Default";

    [SerializeField]
    private int _sortingOrder = 2;

    [Networked]
    private NetworkBool IsConsumed { get; set; }

    /// <summary>Whether replicated loot identity and quantity are ready for interaction.</summary>
    [Networked]
    public NetworkBool IsInitialized { get; private set; }

    /// <summary>Deterministic shared-catalog index replicated to every peer.</summary>
    [Networked]
    public int LootCatalogIndex { get; private set; }

    [Networked]
    private int SynchronizedAmount { get; set; }

    public bool IsAvailable => !IsConsumed;

    public new EntityId Id => new EntityId(unchecked((int)Object.Id.Raw));

    private Collider2D[] _cachedColliders;
    private EntityRegistry _registry;
    private bool _isRegistered;
    private EntityId _registeredId;
    private LootEntry _spawnContentOverride;
    private bool _hasSpawnContentOverride;
    private LootDefinition _resolvedLootDefinition;

    /// <summary>Static loot definition resolved locally from replicated catalog identity.</summary>
    public LootDefinition LootDefinition => _resolvedLootDefinition;

    /// <summary>Replicated quantity delivered by a successful pickup interaction.</summary>
    public int Amount => IsInitialized ? SynchronizedAmount : 0;

    /// <summary>Shared catalog used to translate stable network indices.</summary>
    public LootDefinitionCatalog LootCatalog => _lootCatalog;

    /// <summary>Sorting layer applied to the renderer of every resolved world sprite.</summary>
    public string SortingLayerName => _sortingLayerName;

    /// <summary>Sorting order applied to the renderer of every resolved world sprite.</summary>
    public int SortingOrder => _sortingOrder;

    private void Awake()
    {
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);
        if (_worldRenderer == null)
        {
            _worldRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        ApplyWorldPresentation();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            InitializeAuthoritativeState();
        }

        RefreshResolvedLoot();
        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry != null)
        {
            _registeredId = Id;
            _isRegistered = _registry.TryRegisterEntity(_registeredId, this, _cachedColliders);
        }
    }

    public override void Render()
    {
        RefreshResolvedLoot();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_isRegistered && _registry != null)
        {
            _registry.TryUnregisterEntity(_registeredId, this);
            _isRegistered = false;
        }

        _resolvedLootDefinition = null;
        _spawnContentOverride = default;
        _hasSpawnContentOverride = false;
    }

    /// <summary>
    /// Supplies the loot stack during Fusion's authoritative pre-spawn callback.
    /// The catalog index and quantity are written to replicated state from
    /// <see cref="Spawned"/> before the object is published to proxies.
    /// </summary>
    internal bool TrySetSpawnContentOverride(
        NetworkRunner runner,
        NetworkObject expectedObject,
        in LootEntry entry)
    {
        if (_hasSpawnContentOverride || runner == null || !runner.IsServer ||
            expectedObject == null || expectedObject.gameObject != gameObject ||
            expectedObject.GetComponent<NetworkLootPickup>() != this ||
            !entry.IsValid || _lootCatalog == null ||
            !_lootCatalog.TryGetIndex(entry.LootId, out _))
        {
            return false;
        }

        _spawnContentOverride = entry;
        _hasSpawnContentOverride = true;
        return true;
    }

    public bool CanInteract(in InteractionRequest request)
    {
        if (request.TargetId != Id) return false;
        if (!IsInitialized || _resolvedLootDefinition == null) return false;
        if (SynchronizedAmount <= 0) return false;
        if (!IsAvailable) return false;
        if (request.InteractorId.Value == 0) return false;

        return true;
    }

    public InteractionResult Interact(in InteractionRequest request)
    {
        // 1. Reject if no State Authority
        if (!HasStateAuthority)
        {
            return InteractionResult.Rejected(InteractionFailureReason.MissingStateAuthority);
        }

        // 2. Validate request and availability
        if (!CanInteract(request))
        {
            return InteractionResult.Rejected(InteractionFailureReason.TargetUnavailable);
        }

        // Resolve the destination capability before reserving this consumable source.
        if (_registry == null || !_registry.TryGetLootReceiver(request.InteractorId, out var receiver) || receiver == null)
        {
            return ToInteractionResult(
                LootTransferResult.Rejected(LootTransferFailureReason.DestinationNotFound),
                false);
        }

        var transferRequest = new LootTransferRequest(
            Id,
            request.InteractorId,
            _resolvedLootDefinition.LootId,
            SynchronizedAmount,
            request.SimulationTick);

        // The pickup's reservation prevents two authoritative interactions from
        // delivering the same consumable source while reception is validated.
        IsConsumed = true;
        LootTransferFailureReason failureReason = receiver.ValidateReceive(transferRequest);

        if (failureReason != LootTransferFailureReason.None)
        {
            IsConsumed = false;
            return ToInteractionResult(LootTransferResult.Rejected(failureReason), false);
        }

        // Commit cannot reject after successful prevalidation while State Authority
        // retains control. Any inability to apply is an integration contract violation.
        receiver.CommitReceive(transferRequest);
        if (receiver is ILootPickupFeedbackSink feedbackSink)
        {
            feedbackSink.PublishPickupGrant(transferRequest);
        }
        LootTransferResult transferResult = LootTransferResult.Succeeded(transferRequest);

        Runner.Despawn(Object);
        return ToInteractionResult(transferResult, true);
    }

    private void InitializeAuthoritativeState()
    {
        LootEntry entry = _hasSpawnContentOverride
            ? _spawnContentOverride
            : _lootDefinition != null
                ? new LootEntry(_lootDefinition.LootId, _amount)
                : default;

        if (!entry.IsValid || _lootCatalog == null ||
            !_lootCatalog.TryGetIndex(entry.LootId, out int catalogIndex))
        {
            Debug.LogError(
                $"{nameof(NetworkLootPickup)} could not initialize '{name}' because its loot entry or catalog is invalid.",
                this);
            IsInitialized = false;
            return;
        }

        LootCatalogIndex = catalogIndex;
        SynchronizedAmount = entry.Amount;
        IsConsumed = false;
        IsInitialized = true;
    }

    private void RefreshResolvedLoot()
    {
        if (!IsInitialized || _lootCatalog == null ||
            !_lootCatalog.TryGetByIndex(LootCatalogIndex, out LootDefinition definition))
        {
            _resolvedLootDefinition = null;
            return;
        }

        if (_resolvedLootDefinition == definition)
        {
            return;
        }

        _resolvedLootDefinition = definition;
        if (_worldRenderer != null)
        {
            _worldRenderer.sprite = definition.WorldSprite;
            ApplyWorldPresentation();
        }
    }

    private void ApplyWorldPresentation()
    {
        if (_worldRenderer == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_sortingLayerName))
        {
            _worldRenderer.sortingLayerName = _sortingLayerName;
        }

        _worldRenderer.sortingOrder = _sortingOrder;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_worldRenderer == null)
        {
            _worldRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        ApplyWorldPresentation();
    }
#endif

    private static InteractionResult ToInteractionResult(
        in LootTransferResult transferResult,
        bool isConsumed)
    {
        if (transferResult.Success)
        {
            return InteractionResult.Succeeded(isConsumed);
        }

        return transferResult.FailureReason switch
        {
            LootTransferFailureReason.MissingAuthority =>
                InteractionResult.Rejected(InteractionFailureReason.MissingStateAuthority),
            LootTransferFailureReason.DestinationNotFound =>
                InteractionResult.Rejected(InteractionFailureReason.ReceiverNotFound),
            LootTransferFailureReason.OutOfRange =>
                InteractionResult.Rejected(InteractionFailureReason.OutOfRange),
            _ => InteractionResult.Rejected(InteractionFailureReason.LootRejected)
        };
    }
}
