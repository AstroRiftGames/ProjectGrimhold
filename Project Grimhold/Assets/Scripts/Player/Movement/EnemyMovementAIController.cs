using Fusion;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Consumes Fusion input during network ticks and delegates movement
/// resolution to the kinematic movement motor.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Kinematic2DMovementMotor))]
public sealed class EnemyMovementAIController : NetworkBehaviour
{
    [Min(0f)]
    private float _moveSpeed;
    [SerializeField, Min(0f)] private float _patrolSpeed;
    [SerializeField, Min(1f)] private float _pursuitSpeedMultiplier;
    private float _pursuitSpeed => _patrolSpeed * _pursuitSpeedMultiplier;
    private Vector2 _moveDirection;
    private float _lastDecisionTime;
    private float _decisionInterval = 1f; // Time interval between decisions in seconds
    private bool _isOnPursuit = false;
    private bool _isAttacking = false;

    private Transform[] _targets;
    [SerializeField] private float _LOSDistance;
    [SerializeField] private float _attackRange;
    [SerializeField] private LayerMask _obstacleLayer;

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
            CheckPotentialTargets();
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

        _moveSpeed = _isOnPursuit ? _pursuitSpeed : _patrolSpeed;

        return Vector2.ClampMagnitude(
            decision,
            1f) * _moveSpeed;
    }

    private bool DecideDirection(out Vector2 decision)
    {
        bool hasTarget = HasTarget(out Transform target, out float disToTarget);
        bool hasLOS = HasLOS(target, disToTarget);
        bool onRange = IsInAttackRange(target);

        if(onRange)
        {
            _moveDirection = Vector2.zero;
        }
        else if (hasLOS)
        {
            //Follow target
            _moveDirection = (target.position - transform.position).normalized;
        }
        else if(Time.time >= _lastDecisionTime + _decisionInterval)
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

    private void CheckPotentialTargets()
    {
        Debug.Log("[Targets] Checking for potential targets...");
        foreach (var potentialTarget in FindObjectsByType(typeof(PlayerCharacter)))
        {
            Debug.Log("[Targets] Found potential target: " + potentialTarget.name);
            
            if (_targets == null)
            {
                Debug.Log("[Targets] Initializing targets array.");
                _targets = new Transform[1];
                _targets[0] = potentialTarget.GameObject().transform;
            }
            else
            {
                Debug.Log("[Targets] Expanding targets array.");
                int currentLength = _targets.Length;
                System.Array.Resize(ref _targets, currentLength + 1);
                _targets[currentLength] = potentialTarget.GameObject().transform;
            }
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

    private bool HasTarget(out Transform target, out float disToTarget)
    {
        target = null;
        disToTarget = float.MaxValue;
        if (_targets == null || _targets.Length == 0)
        {
            _isOnPursuit = false;
            return false;
        }
        else
        {
            foreach(var t in _targets)
            {
                if (t == null) continue;
                float distance = Vector2.Distance(transform.position, t.position);
                if (distance < disToTarget)
                {
                    disToTarget = distance;
                    target = t;
                }
            }
        }
        return true;
    }

    private bool HasLOS(Transform target, float disToTarget)
    {
        _isOnPursuit = false;
        
        if (target == null) return false;
        
        bool outOfRange = disToTarget > _LOSDistance;
        if (outOfRange) return false;

        bool blocked = Physics2D.Raycast(transform.position, target.position - transform.position, disToTarget, _obstacleLayer);
        if(blocked) return false;

        _isOnPursuit = true;
        return true;
    }
        
    private bool IsInAttackRange(Transform target)
    {
        _isAttacking = false;
        if (target == null) return false;

        bool isInRange = Vector2.Distance(transform.position, target.position) <= _attackRange;

        if(!isInRange) return false;

        _isAttacking = true;
        return true;
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _LOSDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
#endif
}