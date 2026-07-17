using UnityEngine;

/// <summary>
/// Presenter component responsible for procedural player attack animations (melee swings and ranged arcs).
/// Operates on the presentation layer, subscribing to network events to drive local visual state.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerCombatPresenter : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private PlayerCombatNetworkController _combatController;

    [SerializeField]
    private PlayerAnimatorView _animatorView;

    [SerializeField]
    private Transform _weaponPivot;

    [SerializeField]
    private SpriteRenderer _weaponSpriteRenderer;

    [Header("Attack Presentation")]
    [SerializeField, Min(0.001f)]
    private float _attackDuration = 0.2f;

    [SerializeField]
    private float _attackStartAngle = -45f;

    [SerializeField]
    private float _attackEndAngle = 45f;

    [SerializeField]
    private AnimationCurve _attackAngleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField, Min(0f)]
    private float _weaponHandDistance = 0.2f;

    [SerializeField]
    private int _sortingOrderFront = 10;

    [SerializeField]
    private int _sortingOrderBack = -10;

    [SerializeField]
    private float _verticalSortingThreshold = 0f;

    [SerializeField]
    private bool _hideWeaponAtEnd = true;

    // Base state capture
    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private Vector3 _baseLocalScale;
    private int _baseSortingOrder;
    private bool _baseSpriteRendererEnabled;
    private bool _baseGameObjectActive;

    private bool _hasCapturedBaseState;
    private bool _isSubscribed;

    // Animation runtime state
    private bool _animationActive;
    private Vector2 _attackDirection; // Continuous direction
    private Vector2 _snappedDirection; // Visual 8-way snapped direction
    private float _animationElapsedTime;

    private void Awake()
    {
        CacheDependencies();
    }

    private void OnEnable()
    {
        CaptureBaseState();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        CancelAndRestore();
    }

    private void CacheDependencies()
    {
        if (_combatController == null)
        {
            _combatController = GetComponentInParent<PlayerCombatNetworkController>();
        }

        if (_animatorView == null)
        {
            _animatorView = GetComponentInParent<PlayerAnimatorView>();
        }
    }

    private void CaptureBaseState()
    {
        if (_hasCapturedBaseState)
        {
            return;
        }

        if (_weaponPivot != null)
        {
            _baseLocalPosition = _weaponPivot.localPosition;
            _baseLocalRotation = _weaponPivot.localRotation;
            _baseLocalScale = _weaponPivot.localScale;
        }

        if (_weaponSpriteRenderer != null)
        {
            _baseSortingOrder = _weaponSpriteRenderer.sortingOrder;
            _baseSpriteRendererEnabled = _weaponSpriteRenderer.enabled;
            _baseGameObjectActive = _weaponSpriteRenderer.gameObject.activeSelf;
        }

        _hasCapturedBaseState = true;
    }

    private void Subscribe()
    {
        if (_isSubscribed)
        {
            return;
        }

        if (_combatController != null)
        {
            _combatController.AttackPerformed += OnAttackPerformed;
            _isSubscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_combatController != null)
        {
            _combatController.AttackPerformed -= OnAttackPerformed;
            _isSubscribed = false;
        }
    }

    /// <summary>
    /// Snaps a 2D direction to the nearest 8-way angle (multiples of 45 degrees).
    /// </summary>
    private Vector2 SnapDirectionToEightWays(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return Vector2.right;
        }

        Vector2 norm = direction.normalized;
        float angleRad = Mathf.Atan2(norm.y, norm.x);
        float angleDeg = angleRad * Mathf.Rad2Deg;

        // Round to nearest 45 degrees
        float snappedAngleDeg = Mathf.Round(angleDeg / 45f) * 45f;
        float snappedAngleRad = snappedAngleDeg * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(snappedAngleRad), Mathf.Sin(snappedAngleRad));
    }

    private void OnAttackPerformed(AttackPerformedEvent attackEvent)
    {
        // Cancel any active animation cleanly first
        CancelAndRestore();

        // Start new presentation
        _animationActive = true;
        _attackDirection = attackEvent.Direction.sqrMagnitude > 0.0001f ? attackEvent.Direction.normalized : Vector2.right;
        
        // Compute snapped visual direction
        _snappedDirection = SnapDirectionToEightWays(_attackDirection);
        _animationElapsedTime = 0f;

        // Apply temporal facing to character animator using the 8-way visual direction
        if (_animatorView != null)
        {
            _animatorView.ApplyTemporalFacingDirection(_snappedDirection);
        }

        // Make sure the weapon GameObject/renderer is enabled
        if (_weaponSpriteRenderer != null)
        {
            _weaponSpriteRenderer.gameObject.SetActive(true);
            _weaponSpriteRenderer.enabled = true;
        }

        // Evaluate initial update (e.g. for rotation or ordering)
        EvaluatePresentation(0f);
    }

    private void LateUpdate()
    {
        if (!_animationActive)
        {
            return;
        }

        _animationElapsedTime += Time.deltaTime;
        float duration = Mathf.Max(0.001f, _attackDuration);
        float progress = Mathf.Clamp01(_animationElapsedTime / duration);

        EvaluatePresentation(progress);

        if (progress >= 1f)
        {
            CompletePresentation();
        }
    }

    private void EvaluatePresentation(float progress)
    {
        if (_weaponPivot == null || _weaponSpriteRenderer == null)
        {
            return;
        }

        // Calculate visual snapped angle in degrees
        float snappedAngleRad = Mathf.Atan2(_snappedDirection.y, _snappedDirection.x);
        float snappedAngleDeg = snappedAngleRad * Mathf.Rad2Deg;

        // visual base position incorporating center offset if weapon distance is set
        Vector3 basePos = _baseLocalPosition;
        if (_weaponHandDistance > 0f)
        {
            Vector3 localOffset = new Vector3(_snappedDirection.x, _snappedDirection.y, 0f) * _weaponHandDistance;
            // Convert direction to parent space if necessary. Since WeaponPivot is a direct child of CombatVisuals,
            // local direction matches parent coordinates.
            basePos = _baseLocalPosition + localOffset;
        }

        // Swing interpolation (unified for all attacks)
        float t = _attackAngleCurve.Evaluate(progress);
        float currentSwingAngle = Mathf.Lerp(_attackStartAngle, _attackEndAngle, t);
        float finalAngle = snappedAngleDeg + currentSwingAngle;

        _weaponPivot.localRotation = Quaternion.Euler(0f, 0f, finalAngle);
        _weaponPivot.localPosition = basePos;
        _weaponPivot.localScale = _baseLocalScale;

        // Sorting order based on visual snapped direction and vertical threshold:
        // Snapped angle degrees: Norte is 90, Noreste is 45, Noroeste is 135.
        // If Y is above vertical sorting threshold, render behind.
        if (_snappedDirection.y > _verticalSortingThreshold)
        {
            _weaponSpriteRenderer.sortingOrder = _sortingOrderBack;
        }
        else
        {
            _weaponSpriteRenderer.sortingOrder = _sortingOrderFront;
        }
    }

    private void CompletePresentation()
    {
        _animationActive = false;

        // Restore pivot position, rotation, scale and sorting order immediately on completion
        if (_weaponPivot != null)
        {
            _weaponPivot.localPosition = _baseLocalPosition;
            _weaponPivot.localRotation = _baseLocalRotation;
            _weaponPivot.localScale = _baseLocalScale;
        }

        if (_weaponSpriteRenderer != null)
        {
            _weaponSpriteRenderer.sortingOrder = _baseSortingOrder;
        }

        // Clear temporal facing direction to restore locomotion state
        if (_animatorView != null)
        {
            _animatorView.ClearTemporalFacingDirection();
        }

        // Apply final visibility config
        if (_weaponSpriteRenderer != null)
        {
            if (_hideWeaponAtEnd)
            {
                _weaponSpriteRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// Cancels any active attack presentation and completely restores the weapon pivot
    /// and weapon sprite renderer to their original base states.
    /// This method is idempotent.
    /// </summary>
    public void CancelAndRestore()
    {
        _animationActive = false;

        if (_animatorView != null)
        {
            _animatorView.ClearTemporalFacingDirection();
        }

        if (!_hasCapturedBaseState)
        {
            return;
        }

        if (_weaponPivot != null)
        {
            _weaponPivot.localPosition = _baseLocalPosition;
            _weaponPivot.localRotation = _baseLocalRotation;
            _weaponPivot.localScale = _baseLocalScale;
        }

        if (_weaponSpriteRenderer != null)
        {
            _weaponSpriteRenderer.sortingOrder = _baseSortingOrder;
            _weaponSpriteRenderer.enabled = _baseSpriteRendererEnabled;
            // Only modify active self if gameObject is active, but do not reactivate a parent-disabled presenter
            if (gameObject.activeInHierarchy)
            {
                _weaponSpriteRenderer.gameObject.SetActive(_baseGameObjectActive);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheDependencies();
    }
#endif
}
