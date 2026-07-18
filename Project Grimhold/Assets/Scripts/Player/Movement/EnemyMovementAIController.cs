using Fusion;
using UnityEngine;

/// <summary>
/// Consumes Fusion input during network ticks and delegates movement
/// resolution to the kinematic movement motor.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Kinematic2DMovementMotor))]
public sealed class EnemyMovementAIController : NetworkBehaviour
{
    [SerializeField, Min(0f)]
    private float _moveSpeed = 3f;
    private Vector2 _moveDirection;
    private float _lastDecisionTime;
    private float _decisionInterval = 1f; // Time interval between decisions in seconds

    [SerializeField]
    private Kinematic2DMovementMotor _movementMotor;

    private bool _dependenciesValid;

    private const float ValidDirectionSqrThreshold = 0.0001f;
    private const float ValidMovementSqrThreshold = 0.000001f;

    [SerializeField]
    private Vector2 _defaultFacingDirection = Vector2.down;

    [Networked]
    public NetworkBool IsControlEnabled { get; private set; }

    [Networked]
    public Vector2 FacingDirection { get; private set; }

    [Networked]
    public NetworkBool IsMoving { get; private set; }

    private CharacterBase _characterBase;

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

            Vector2 initialFacing = _defaultFacingDirection.normalized;
            if (initialFacing.sqrMagnitude < 0.001f)
            {
                initialFacing = Vector2.down;
            }
            FacingDirection = initialFacing;
            IsMoving = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!_dependenciesValid)
        {
            return;
        }

        IsMoving = false;

        Vector2 moveDirection = ReadMoveDirection();

        bool canMove = IsControlEnabled && (_characterBase == null || _characterBase.IsAlive);

        if (canMove && moveDirection.sqrMagnitude > ValidDirectionSqrThreshold)
        {
            FacingDirection = moveDirection.normalized;
        }

        Vector2 displacement = canMove
            ? moveDirection * _moveSpeed * Runner.DeltaTime
            : Vector2.zero;

        Vector2 appliedDisplacement = _movementMotor.Move(displacement);

        if (appliedDisplacement.sqrMagnitude > ValidMovementSqrThreshold)
        {
            IsMoving = true;
        }
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
        if (!DecideDirection(out Vector2 decision))
        {
            return Vector2.zero;
        }

        return Vector2.ClampMagnitude(
            decision,
            1f);
    }

    private bool DecideDirection(out Vector2 decision)
    {
        // Implement your AI logic here to determine the move direction.
        // For example, you can use a simple random movement or follow a target.
        // This is just a placeholder for demonstration purposes.
        if(Time.time >= _lastDecisionTime + _decisionInterval)
        {
            // Randomly choose a direction
            _moveDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            _lastDecisionTime = Time.time;
        }
        decision = _moveDirection;
        return true;
    }

    private void CacheDependencies()
    {
        if (_movementMotor == null)
        {
            _movementMotor =
                GetComponent<Kinematic2DMovementMotor>();
        }

        if (_characterBase == null)
        {
            _characterBase = GetComponent<CharacterBase>();
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