using UnityEngine;

/// <summary>
/// Updates character animation parameters based on the networked player movement state.
/// 
/// This component belongs to the presentation layer. It reads values from
/// the movement simulation and updates the Unity Animator without participating
/// in network simulation, prediction, or input gathering.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyAnimatorView : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private EnemyMovementAIController _movementController;

    private int _moveXHash;
    private int _moveYHash;
    private int _isMovingHash;

    private bool _hashesInitialized;
    private Vector2? _temporalFacingDirection;
    private bool _isDefeated;

    private void Awake()
    {
        InitializeHashes();
        CacheDependencies();
    }

    private void OnDisable()
    {
        _temporalFacingDirection = null;
        _isDefeated = false;
    }

    private void LateUpdate()
    {
        if (_movementController == null || _animator == null)
        {
            return;
        }

        if (!_hashesInitialized)
        {
            InitializeHashes();
        }

        Vector2 facing;
        bool isMoving;

        if (_isDefeated)
        {
            facing = _movementController.FacingDirection;
            isMoving = false;
        }
        else if (_temporalFacingDirection.HasValue)
        {
            facing = _temporalFacingDirection.Value;
            isMoving = false;
        }
        else
        {
            facing = _movementController.FacingDirection;
            isMoving = _movementController.IsMoving;
        }

        if (facing.sqrMagnitude < 0.001f)
        {
            facing = Vector2.down;
        }

        _animator.SetFloat(_moveXHash, facing.x);
        _animator.SetFloat(_moveYHash, facing.y);
        _animator.SetBool(_isMovingHash, isMoving);
    }

    /// <summary>
    /// Sets the defeated visual state of the animator, halting locomotion animation.
    /// </summary>
    /// <param name="defeated">True if the player is defeated, false otherwise.</param>
    public void SetDefeated(bool defeated)
    {
        _isDefeated = defeated;
        if (defeated)
        {
            _temporalFacingDirection = null;
        }
    }

    /// <summary>
    /// Applies a temporal facing direction (e.g. for attack presentation) that overrides locomotion facing direction.
    /// During temporal facing, the movement state is forced to IsMoving = false.
    /// </summary>
    /// <param name="direction">The direction to face.</param>
    public void ApplyTemporalFacingDirection(Vector2 direction)
    {
        if (_isDefeated)
        {
            return;
        }
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }
        _temporalFacingDirection = direction.normalized;
    }

    /// <summary>
    /// Clears any temporal facing direction, returning the animator to standard locomotion state.
    /// </summary>
    public void ClearTemporalFacingDirection()
    {
        _temporalFacingDirection = null;
    }

    private void InitializeHashes()
    {
        _moveXHash = Animator.StringToHash("MoveX");
        _moveYHash = Animator.StringToHash("MoveY");
        _isMovingHash = Animator.StringToHash("IsMoving");
        _hashesInitialized = true;
    }

    private void CacheDependencies()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }

        if (_movementController == null)
        {
            _movementController = GetComponentInParent<EnemyMovementAIController>();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheDependencies();
    }

    private void Reset()
    {
        CacheDependencies();
    }
#endif
}
