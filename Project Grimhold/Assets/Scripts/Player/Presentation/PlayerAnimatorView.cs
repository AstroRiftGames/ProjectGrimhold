using UnityEngine;

/// <summary>
/// Updates character animation parameters based on the networked player movement state.
/// 
/// This component belongs to the presentation layer. It reads values from
/// the movement simulation and updates the Unity Animator without participating
/// in network simulation, prediction, or input gathering.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerAnimatorView : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private PlayerMovementNetworkController _movementController;

    private int _moveXHash;
    private int _moveYHash;
    private int _isMovingHash;

    private bool _hashesInitialized;

    private void Awake()
    {
        InitializeHashes();
        CacheDependencies();
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

        Vector2 facing = _movementController.FacingDirection;
        if (facing.sqrMagnitude < 0.001f)
        {
            facing = Vector2.down;
        }

        _animator.SetFloat(_moveXHash, facing.x);
        _animator.SetFloat(_moveYHash, facing.y);
        _animator.SetBool(_isMovingHash, _movementController.IsMoving);
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
            _movementController = GetComponentInParent<PlayerMovementNetworkController>();
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
