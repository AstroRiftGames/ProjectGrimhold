using Fusion;
using UnityEngine;

public sealed class PlayerInputReader : MonoBehaviour
{
    [Header("Look")]
    [SerializeField, Min(0f)]
    private float _lookSensitivity = 0.1f;

    [SerializeField]
    private Vector2 _pitchLimits = new(-89f, 89f);

    private PlayerInputActions _inputActions;

    private Vector2 _moveDirection;

    private Vector2 _lookRotation;

    private NetworkButtons _buttons;
    private bool _resetAccumulatedButtons;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.Gameplay.Enable();
    }

    private void Update()
    {
        ResetAccumulatedButtonsIfRequired();
        ReadMovement();
    }

    private void OnDisable()
    {
        _inputActions.Gameplay.Disable();
        ResetInputState();
    }

    private void OnDestroy()
    {
        _inputActions.Dispose();
    }

    public PlayerNetworkInput ConsumeNetworkInput()
    {
        _resetAccumulatedButtons = true;

        return new PlayerNetworkInput
        {
            MoveDirection = _moveDirection,
            LookRotation = _lookRotation,
            Buttons = _buttons
        };
    }

    private void ReadMovement()
    {
        _moveDirection =
            _inputActions.Gameplay.Move.ReadValue<Vector2>();
    }

    private void AccumulateButton(
        PlayerInputButton button,
        bool isPressed,
        bool wasPressedThisFrame)
    {
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
        _buttons = default;
        _resetAccumulatedButtons = false;
    }

    private static float NormalizeAngle(float angle)
    {
        return Mathf.Repeat(angle + 180f, 360f) - 180f;
    }
}
