using Fusion;
using UnityEngine;

/// <summary>
/// Captures local device input and exposes it as gameplay intentions.
///
/// This component does not execute movement, attacks or any other gameplay
/// action. Fusion consumes the accumulated state through
/// <see cref="ConsumeNetworkInput"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerInputReader : MonoBehaviour
{
    [Header("Aim")]
    [SerializeField]
    private Camera _worldCamera;

    [SerializeField]
    private float _aimPlaneZ;

    private PlayerInputActions _inputActions;

    private Vector2 _moveDirection;
    private Vector2 _aimWorldPosition;

    private NetworkButtons _buttons;
    private NetworkButtons _pendingButtons;
    private bool _resetAccumulatedButtons;

    private void Awake()
    {
        CacheDependencies();

        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.Gameplay.Interact.performed += OnInteractPerformed;
        _inputActions.Gameplay.Enable();
    }

    private void Update()
    {
        ResetAccumulatedButtonsIfRequired();

        ReadMovement();
        ReadAimWorldPosition();
        ReadPrimaryAttack();
    }

    private void OnDisable()
    {
        _inputActions.Gameplay.Interact.performed -= OnInteractPerformed;
        _inputActions.Gameplay.Disable();
        ResetInputState();
    }

    private void OnDestroy()
    {
        _inputActions.Dispose();
    }

    private void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        _pendingButtons.Set(PlayerInputButton.Interact, true);
    }

    /// <summary>
    /// Returns the latest local intentions accumulated since Fusion's
    /// previous input collection.
    /// </summary>
    public PlayerNetworkInput ConsumeNetworkInput()
    {
        // The reset is deferred until the next Update. This preserves a
        // latched press when Fusion requests input more than once in the
        // same rendered frame.
        _resetAccumulatedButtons = true;

        NetworkButtons combinedButtons = _buttons;

        if (_pendingButtons.IsSet(PlayerInputButton.Interact))
        {
            combinedButtons.Set(PlayerInputButton.Interact, true);
        }

        PlayerNetworkInput input = new PlayerNetworkInput
        {
            MoveDirection = _moveDirection,
            AimWorldPosition = _aimWorldPosition,
            Buttons = combinedButtons
        };

        _pendingButtons.Set(PlayerInputButton.Interact, false);

        return input;
    }

    private void ReadMovement()
    {
        _moveDirection =
            _inputActions.Gameplay.Move.ReadValue<Vector2>();
    }

    private void ReadAimWorldPosition()
    {
        if (_worldCamera == null)
        {
            return;
        }

        Vector2 screenPosition =
            _inputActions.Gameplay.AimPosition.ReadValue<Vector2>();

        Transform cameraTransform = _worldCamera.transform;

        Vector3 aimPlanePoint =
            new(0f, 0f, _aimPlaneZ);

        float distanceToAimPlane = Vector3.Dot(
            aimPlanePoint - cameraTransform.position,
            cameraTransform.forward);

        if (distanceToAimPlane < 0f)
        {
            return;
        }

        Vector3 screenPoint = new(
            screenPosition.x,
            screenPosition.y,
            distanceToAimPlane);

        Vector3 worldPosition =
            _worldCamera.ScreenToWorldPoint(screenPoint);

        _aimWorldPosition = new Vector2(
            worldPosition.x,
            worldPosition.y);
    }

    private void ReadPrimaryAttack()
    {
        var primaryAttackAction =
            _inputActions.Gameplay.PrimaryAttack;

        AccumulateButton(
            PlayerInputButton.PrimaryAttack,
            primaryAttackAction.IsPressed(),
            primaryAttackAction.WasPressedThisFrame());
    }

    private void AccumulateButton(
        PlayerInputButton button,
        bool isPressed,
        bool wasPressedThisFrame)
    {
        // IsPressed transports the held state for automatic attacks.
        // WasPressedThisFrame preserves a very short tap until Fusion
        // consumes the input.
        if (isPressed || wasPressedThisFrame)
        {
            _buttons.Set(button, true);
        }
    }

    private void ResetAccumulatedButtonsIfRequired()
    {
        if (!_resetAccumulatedButtons)
        {
            return;
        }

        _buttons = default;
        _resetAccumulatedButtons = false;
    }

    private void ResetInputState()
    {
        _moveDirection = Vector2.zero;
        _aimWorldPosition = Vector2.zero;
        _buttons = default;
        _pendingButtons = default;
        _resetAccumulatedButtons = false;
    }

    private void CacheDependencies()
    {
        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        CacheDependencies();
    }

    private void OnValidate()
    {
        CacheDependencies();
    }
#endif
}