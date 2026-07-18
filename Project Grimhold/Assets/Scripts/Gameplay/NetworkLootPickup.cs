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

    [Networked]
    private NetworkBool IsConsumed { get; set; }

    public bool IsAvailable => !IsConsumed;

    public new EntityId Id => new EntityId(unchecked((int)Object.Id.Raw));

    private Collider2D[] _cachedColliders;
    private EntityRegistry _registry;
    private bool _isRegistered;
    private EntityId _registeredId;

    public LootDefinition LootDefinition => _lootDefinition;
    public int Amount => _amount;

    private void Awake()
    {
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);
    }

    public override void Spawned()
    {
        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry != null)
        {
            _registeredId = Id;
            _isRegistered = _registry.TryRegisterEntity(_registeredId, this, _cachedColliders);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_isRegistered && _registry != null)
        {
            _registry.TryUnregisterEntity(_registeredId, this);
            _isRegistered = false;
        }
    }

    public bool CanInteract(in InteractionRequest request)
    {
        if (request.TargetId != Id) return false;
        if (_lootDefinition == null) return false;
        if (_amount <= 0) return false;
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

        // 3. Resolve ILootReceiver using registry and interactorId
        if (_registry == null || !_registry.TryGetLootReceiver(request.InteractorId, out var receiver) || receiver == null)
        {
            return InteractionResult.Rejected(InteractionFailureReason.ReceiverNotFound);
        }

        // 4. Temporarily mark pickup as consumed before granting loot
        IsConsumed = true;

        // 5. Build LootGrantRequest
        var grantRequest = new LootGrantRequest(
            Id,
            request.InteractorId,
            _lootDefinition.LootId,
            _amount,
            request.SimulationTick
        );

        // 6. Execute grant
        var grantResult = receiver.TryGrantLoot(grantRequest);

        // 7. Handle grant failure
        if (!grantResult.Success)
        {
            IsConsumed = false; // Restore availability
            return InteractionResult.Rejected(InteractionFailureReason.LootRejected);
        }

        // 8. Despawn on success
        Runner.Despawn(Object);

        // 9. Return success
        return InteractionResult.Succeeded(true);
    }
}
