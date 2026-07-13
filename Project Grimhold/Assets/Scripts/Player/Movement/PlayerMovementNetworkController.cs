using Fusion;
using UnityEngine;

/// <summary>
/// Consumes Fusion input during network ticks and delegates movement
/// resolution to the kinematic movement motor.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Kinematic2DMovementMotor))]
public sealed class PlayerMovementNetworkController : NetworkBehaviour
{
    [SerializeField, Min(0f)]
    private float _moveSpeed = 5f;

    [SerializeField]
    private Kinematic2DMovementMotor _movementMotor;

    private bool _dependenciesValid;

    [Networked]
    public NetworkBool IsControlEnabled { get; private set; }

    public Vector2 LastMoveDirection { get; private set; }

    private void Awake()
    {
        CacheDependencies();
    }

    public override void Spawned()
    {
        CacheDependencies();
        _dependenciesValid = ValidateDependencies();

        if (HasStateAuthority)
        {
            IsControlEnabled = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!_dependenciesValid)
        {
            return;
        }

        Vector2 moveDirection = ReadMoveDirection();

        LastMoveDirection = moveDirection;

        Vector2 displacement = IsControlEnabled
            ? moveDirection * _moveSpeed * Runner.DeltaTime
            : Vector2.zero;

        _movementMotor.Move(displacement);
    }

    public bool TrySetControlEnabled(bool enabled)
    {
        if (!HasStateAuthority)
        {
            return false;
        }

        IsControlEnabled = enabled;
        return true;
    }

    private Vector2 ReadMoveDirection()
    {
        if (!GetInput(out PlayerNetworkInput input))
        {
            return Vector2.zero;
        }

        return Vector2.ClampMagnitude(
            input.MoveDirection,
            1f);
    }

    private void CacheDependencies()
    {
        if (_movementMotor == null)
        {
            _movementMotor =
                GetComponent<Kinematic2DMovementMotor>();
        }
    }

    private bool ValidateDependencies()
    {
        if (_movementMotor != null)
        {
            return true;
        }

        Debug.LogError(
            $"{nameof(PlayerMovementNetworkController)} requires " +
            $"{nameof(Kinematic2DMovementMotor)}.",
            this);

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _moveSpeed = Mathf.Max(0f, _moveSpeed);

        if (_movementMotor == null)
        {
            _movementMotor =
                GetComponent<Kinematic2DMovementMotor>();
        }
    }
#endif
}