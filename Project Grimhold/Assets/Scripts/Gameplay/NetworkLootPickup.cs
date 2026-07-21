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
            _lootDefinition.LootId,
            _amount,
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
        LootTransferResult transferResult = LootTransferResult.Succeeded(transferRequest);

        Runner.Despawn(Object);
        return ToInteractionResult(transferResult, true);
    }

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
