using Fusion;
using UnityEngine;

/// <summary>
/// Exposes a synchronized loot container through the shared interaction pipeline.
/// It owns only the interactable capability; the co-located container owns loot state and colliders.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkLootContainer))]
public sealed class NetworkLootContainerInteractable : NetworkBehaviour, IInteractable
{
    private NetworkLootContainer _container;
    private EntityRegistry _registry;
    private EntityId _registeredId;
    private bool _compositionValid;
    private bool _isRegistered;
    private bool _reportedInvalidComposition;

    public new EntityId Id => Object != null
        ? new EntityId(unchecked((int)Object.Id.Raw))
        : default;

    private void Awake()
    {
        ValidateComposition();
    }

    public override void Spawned()
    {
        if (!ValidateComposition())
        {
            return;
        }

        _registry = Runner.GetComponent<EntityRegistry>();
        _registeredId = Id;
        if (_registry == null || !_registry.TryRegisterInteractable(_registeredId, this))
        {
            Debug.LogError(
                $"{nameof(NetworkLootContainerInteractable)}: Failed to register interactable '{name}' with ID {_registeredId}.",
                this);
            _registry = null;
            return;
        }

        _isRegistered = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_isRegistered && _registry != null)
        {
            _registry.TryUnregisterInteractable(_registeredId, this);
        }

        _isRegistered = false;
        _registry = null;
    }

    public bool CanInteract(in InteractionRequest request)
    {
        return _compositionValid && _isRegistered && request.InteractorId.Value != 0 &&
            request.TargetId.Value != 0 && request.TargetId == Id &&
            _container.IsInitialized && _container.IsAvailable;
    }

    public InteractionResult Interact(in InteractionRequest request)
    {
        if (!HasStateAuthority)
        {
            return InteractionResult.Rejected(InteractionFailureReason.MissingStateAuthority);
        }

        return CanInteract(request)
            ? InteractionResult.Succeeded(isConsumed: false)
            : InteractionResult.Rejected(InteractionFailureReason.TargetUnavailable);
    }

    private bool ValidateComposition()
    {
        _container = GetComponent<NetworkLootContainer>();
        NetworkObject rootObject = GetComponent<NetworkObject>();
        _compositionValid = _container != null && rootObject != null &&
            (_container.Object == null || ReferenceEquals(_container.Object, rootObject)) &&
            (Object == null || ReferenceEquals(Object, rootObject));

        if (_compositionValid || _reportedInvalidComposition)
        {
            return _compositionValid;
        }

        _reportedInvalidComposition = true;
        Debug.LogError(
            $"{nameof(NetworkLootContainerInteractable)} requires {nameof(NetworkLootContainer)} and {nameof(NetworkObject)} on the same root and network identity.",
            this);
        enabled = false;
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            _container = GetComponent<NetworkLootContainer>();
        }
    }
#endif
}
