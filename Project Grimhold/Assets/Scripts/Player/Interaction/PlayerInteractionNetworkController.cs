using System;
using Fusion;
using UnityEngine;

/// <summary>
/// Network component responsible for processing player interaction intentions.
/// Delegates targets validation and execution to the pure logical resolver.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerInteractionNetworkController : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private PlayerInteractionConfig _config;

    [SerializeField]
    private MonoBehaviour _characterSource;

    [SerializeField]
    private MonoBehaviour _querySource;

    [SerializeField]
    private PlayerMovementNetworkController _movementController;

    [SerializeField]
    private Transform _interactionOrigin;

    private ICharacter _character;
    private IInteractionTargetQuery _query;
    private EntityRegistry _registry;
    private bool _dependenciesValid;
    private int _observedInteractionSequence;

    [Networked]
    private NetworkButtons PreviousButtons { get; set; }

    [Networked]
    private int InteractionSequence { get; set; }

    [Networked]
    private int LastInteractionTargetIdValue { get; set; }

    [Networked]
    private int LastInteractionTick { get; set; }

    [Networked]
    private NetworkBool LastInteractionSucceeded { get; set; }

    [Networked]
    private NetworkBool LastInteractionConsumed { get; set; }

    /// <summary>
    /// Local event raised during Render when a successful interaction is detected.
    /// </summary>
    public event Action<InteractionPresentationEvent> InteractionResolved;

    private void Awake()
    {
        CacheDependencies();
    }

    public override void Spawned()
    {
        CacheDependencies();
        _dependenciesValid = ValidateDependencies();

        _observedInteractionSequence = InteractionSequence;

        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry == null)
        {
            Debug.LogError($"{nameof(PlayerInteractionNetworkController)}: EntityRegistry was not found on the NetworkRunner.", this);
            _dependenciesValid = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (!_dependenciesValid)
        {
            return;
        }

        if (!GetInput(out PlayerNetworkInput input))
        {
            return;
        }

        NetworkButtons currentButtons = input.Buttons;
        bool interactPressed = currentButtons.WasPressed(PreviousButtons, PlayerInputButton.Interact);

        PreviousButtons = currentButtons;

        if (!interactPressed)
        {
            return;
        }

        // Control check
        if (_movementController == null || !_movementController.IsControlEnabled)
        {
            return;
        }

        // Death / Alive check
        if (_character == null || !_character.IsAlive)
        {
            return;
        }

        TryProcessInteraction();
    }

    private void TryProcessInteraction()
    {
        Vector2 originPos = _interactionOrigin != null ? (Vector2)_interactionOrigin.position : (Vector2)transform.position;

        InteractionTargetQuery targetQuery = new InteractionTargetQuery(
            _character.Id,
            originPos,
            _config.MaximumDistance,
            _config.TargetLayerMask
        );

        var candidates = _query.FindTargets(in targetQuery);

        bool executed = InteractionResolver.TryResolve(
            _character.Id,
            Runner.Tick,
            _config.MaximumDistance,
            candidates,
            _registry.TryGetInteractable,
            out var resolvedRequest,
            out var resolvedResult
        );

        if (executed)
        {
            LastInteractionTargetIdValue = resolvedRequest.TargetId.Value;
            LastInteractionTick = resolvedRequest.SimulationTick;
            LastInteractionSucceeded = resolvedResult.Success;
            LastInteractionConsumed = resolvedResult.IsConsumed;

            // Increment sequence to notify local presentation
            InteractionSequence++;
        }
    }

    public override void Render()
    {
        if (!_dependenciesValid)
        {
            return;
        }

        if (InteractionSequence != _observedInteractionSequence)
        {
            InteractionPresentationEvent evt = new InteractionPresentationEvent(
                _character.Id,
                new EntityId(LastInteractionTargetIdValue),
                LastInteractionTick,
                LastInteractionSucceeded,
                LastInteractionConsumed
            );

            InteractionResolved?.Invoke(evt);
            _observedInteractionSequence = InteractionSequence;
        }
    }

    private void CacheDependencies()
    {
        if (_characterSource != null)
        {
            _character = _characterSource as ICharacter;
        }
        else
        {
            _character = GetComponent<ICharacter>();
        }

        if (_querySource != null)
        {
            _query = _querySource as IInteractionTargetQuery;
        }
        else
        {
            _query = GetComponent<IInteractionTargetQuery>();
        }

        if (_movementController == null)
        {
            _movementController = GetComponent<PlayerMovementNetworkController>();
        }

        if (_interactionOrigin == null)
        {
            _interactionOrigin = transform;
        }
    }

    private bool ValidateDependencies()
    {
        if (_config == null)
        {
            Debug.LogError($"{nameof(PlayerInteractionNetworkController)}: Configuration asset is not assigned.", this);
            return false;
        }

        if (_character == null)
        {
            Debug.LogError($"{nameof(PlayerInteractionNetworkController)}: No component implementing {nameof(ICharacter)} is assigned.", this);
            return false;
        }

        if (_query == null)
        {
            Debug.LogError($"{nameof(PlayerInteractionNetworkController)}: No component implementing {nameof(IInteractionTargetQuery)} is assigned.", this);
            return false;
        }

        if (_movementController == null)
        {
            Debug.LogError($"{nameof(PlayerInteractionNetworkController)}: PlayerMovementNetworkController is not assigned.", this);
            return false;
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheDependencies();
    }
#endif
}
