using UnityEngine;

/// <summary>
/// Presenter component responsible for procedural character defeat animation and visual pose.
/// Operates on the presentation layer, observing the networked health state of a CharacterBase.
/// When the character dies, handles the defeat transition and coordinates with other presenters.
/// </summary>
[DisallowMultipleComponent]
public abstract class DefeatPresenterBase : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField]
    private CharacterBase _characterBase;

    [SerializeField]
    private MonoBehaviour _animatorViewSource;

    [SerializeField]
    private CombatPresenterBase _combatPresenter;

    [SerializeField]
    private DamageFeedbackPresenter _damageFeedbackPresenter;

    [SerializeField]
    private SpriteRenderer _weaponSpriteRenderer;

    [SerializeField]
    private Transform _visualTransform;

    [SerializeField]
    private SpriteRenderer[] _spriteRenderers;

    [SerializeField]
    private GameObject _bodyVisualRoot;

    [SerializeField]
    private GameObject _combatVisualRoot;

    [Header("Defeat Visuals")]
    [SerializeField, Min(0.001f)]
    private float _transitionDuration = 0.5f;

    [SerializeField, Min(0f)]
    private float _hideDelayAfterTransition = 1.5f;

    [SerializeField]
    private float _targetRotationAngle = 90f;

    [SerializeField]
    private AnimationCurve _rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField, Range(0f, 1f)]
    private float _targetAlpha = 0.6f;

    [SerializeField]
    private AnimationCurve _alphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private IAnimatorController _animatorView;

    // Runtime state
    private bool _isDefeated;
    private float _elapsedTime;
    private bool _transitionActive;
    private float _timeSinceTransitionCompleted;
    private bool _visualsHidden;

    // Cached base states
    private Quaternion _baseLocalRotation;
    private float[] _originalAlphas;
    private Color[] _originalColors;
    private bool _baseBodyVisualActive;
    private bool _baseCombatVisualActive;
    private bool _hasCapturedBaseState;
    private bool _isInitialized;

    /// <summary>
    /// Controls whether the body presentation disappears after the defeat transition.
    /// Persistent corpses override this while retaining the shared combat-visual cleanup.
    /// </summary>
    protected virtual bool HideBodyVisualAfterTransition => true;

    protected virtual void OnEnable()
    {
        CacheDependencies();
        CaptureBaseState();
        InitializeDeathTracking();
    }

    protected virtual void OnDisable()
    {
        CancelAndRestore();
        _isInitialized = false;
    }

    protected virtual void LateUpdate()
    {
        if (_characterBase == null)
        {
            return;
        }

        if (!_isInitialized)
        {
            if (_characterBase.Object != null && _characterBase.Object.IsValid)
            {
                _isInitialized = true;
                CaptureBaseState();

                if (!_characterBase.IsAlive)
                {
                    TriggerDefeat(true);
                }
            }
            return;
        }

        bool currentlyAlive = _characterBase.IsAlive;

        if (!currentlyAlive && !_isDefeated)
        {
            TriggerDefeat(false);
        }

        if (_transitionActive)
        {
            UpdateTransition();
        }

        if (_isDefeated && !_transitionActive && !_visualsHidden)
        {
            _timeSinceTransitionCompleted += Time.deltaTime;
            if (_timeSinceTransitionCompleted >= _hideDelayAfterTransition)
            {
                HideVisuals();
            }
        }
    }

    protected virtual void CacheDependencies()
    {
        if (_characterBase == null)
        {
            _characterBase = GetComponentInParent<CharacterBase>();
        }

        if (_animatorViewSource != null)
        {
            _animatorView = _animatorViewSource as IAnimatorController;
        }

        if (_animatorView == null)
        {
            _animatorView = GetComponentInParent<IAnimatorController>();
        }

        if (_combatPresenter == null)
        {
            _combatPresenter = GetComponentInChildren<CombatPresenterBase>();
        }

        if (_damageFeedbackPresenter == null)
        {
            _damageFeedbackPresenter = GetComponentInChildren<DamageFeedbackPresenter>();
        }
    }

    private void CaptureBaseState()
    {
        if (_hasCapturedBaseState)
        {
            return;
        }

        if (_visualTransform != null)
        {
            _baseLocalRotation = _visualTransform.localRotation;
        }

        if (_bodyVisualRoot != null)
        {
            _baseBodyVisualActive = _bodyVisualRoot.activeSelf;
        }

        if (_combatVisualRoot != null)
        {
            _baseCombatVisualActive = _combatVisualRoot.activeSelf;
        }

        if (_spriteRenderers != null && _spriteRenderers.Length > 0)
        {
            _originalColors = new Color[_spriteRenderers.Length];
            _originalAlphas = new float[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null)
                {
                    _originalColors[i] = _spriteRenderers[i].color;
                    _originalAlphas[i] = _spriteRenderers[i].color.a;
                }
            }
        }

        _hasCapturedBaseState = true;
    }

    private void InitializeDeathTracking()
    {
        if (_characterBase != null && _characterBase.Object != null && _characterBase.Object.IsValid)
        {
            _isInitialized = true;
            if (!_characterBase.IsAlive)
            {
                TriggerDefeat(true);
            }
        }
        else
        {
            _isInitialized = false;
        }
    }

    private void HideVisuals()
    {
        if (_visualsHidden)
        {
            return;
        }
        _visualsHidden = true;
        if (HideBodyVisualAfterTransition && _bodyVisualRoot != null)
        {
            _bodyVisualRoot.SetActive(false);
        }
        if (_combatVisualRoot != null)
        {
            _combatVisualRoot.SetActive(false);
        }
    }

    private void TriggerDefeat(bool instant)
    {
        _isDefeated = true;
        _timeSinceTransitionCompleted = 0f;
        _visualsHidden = false;

        if (_combatPresenter != null)
        {
            _combatPresenter.CancelAndRestore();
        }

        if (_damageFeedbackPresenter != null)
        {
            _damageFeedbackPresenter.CancelAndRestore();
        }

        if (_weaponSpriteRenderer != null)
        {
            _weaponSpriteRenderer.enabled = false;
        }

        if (_animatorView != null)
        {
            _animatorView.SetDefeated(true);
        }

        if (instant)
        {
            _transitionActive = false;
            _elapsedTime = _transitionDuration;
            ApplyDefeatState(1f);
            HideVisuals();
        }
        else
        {
            _transitionActive = true;
            _elapsedTime = 0f;
            ApplyDefeatState(0f);
        }
    }

    private void UpdateTransition()
    {
        _elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsedTime / _transitionDuration);

        ApplyDefeatState(progress);

        if (_elapsedTime >= _transitionDuration)
        {
            _transitionActive = false;
        }
    }

    private void ApplyDefeatState(float progress)
    {
        if (!_hasCapturedBaseState)
        {
            return;
        }

        float rotT = _rotationCurve != null ? _rotationCurve.Evaluate(progress) : progress;
        float currentAngle = Mathf.Lerp(0f, _targetRotationAngle, rotT);

        if (_visualTransform != null)
        {
            _visualTransform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, currentAngle);
        }

        float alphaT = _alphaCurve != null ? _alphaCurve.Evaluate(progress) : progress;
        if (_spriteRenderers != null && _originalColors != null)
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null && i < _originalColors.Length)
                {
                    Color col = _originalColors[i];
                    float targetA = _originalAlphas[i] * _targetAlpha;
                    col.a = Mathf.Lerp(_originalAlphas[i], targetA, alphaT);
                    _spriteRenderers[i].color = col;
                }
            }
        }
    }

    /// <summary>
    /// Cancels defeat presentation and restores base rotation, colors, and animation states.
    /// </summary>
    public void CancelAndRestore()
    {
        _isDefeated = false;
        _transitionActive = false;
        _elapsedTime = 0f;
        _timeSinceTransitionCompleted = 0f;
        _visualsHidden = false;

        if (_animatorView != null)
        {
            _animatorView.SetDefeated(false);
        }

        if (!_hasCapturedBaseState)
        {
            return;
        }

        if (_bodyVisualRoot != null)
        {
            _bodyVisualRoot.SetActive(_baseBodyVisualActive);
        }
        if (_combatVisualRoot != null)
        {
            _combatVisualRoot.SetActive(_baseCombatVisualActive);
        }

        if (_visualTransform != null)
        {
            _visualTransform.localRotation = _baseLocalRotation;
        }

        if (_spriteRenderers != null && _originalColors != null)
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] != null && i < _originalColors.Length)
                {
                    _spriteRenderers[i].color = _originalColors[i];
                }
            }
        }
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        CacheDependencies();
    }
#endif
}
