using UnityEngine;

/// <summary>
/// Configuración inmutable de datos compartidos para ataques melee.
/// </summary>
[CreateAssetMenu(fileName = "MeleeAttackConfig", menuName = "Grimhold/Combat/MeleeAttackConfig")]
public sealed class MeleeAttackConfig : ScriptableObject
{
    [SerializeField, Min(0f)]
    private float _damage = 10f;

    [SerializeField]
    private DamageType _damageType = DamageType.Physical;

    [SerializeField, Min(0f)]
    private float _cooldownSeconds = 0.5f;

    [SerializeField]
    private AttackInputMode _inputMode = AttackInputMode.Press;

    [SerializeField, Min(0.1f)]
    private float _range = 1f;

    [SerializeField, Min(0.1f)]
    private float _radius = 0.5f;

    [SerializeField, Min(1)]
    private int _maximumTargets = 1;

    [SerializeField]
    private LayerMask _targetLayerMask;

    public float Damage => _damage;
    public DamageType DamageType => _damageType;
    public float CooldownSeconds => _cooldownSeconds;
    public AttackInputMode InputMode => _inputMode;
    public float Range => _range;
    public float Radius => _radius;
    public int MaximumTargets => _maximumTargets;
    public LayerMask TargetLayerMask => _targetLayerMask;

    private void OnValidate()
    {
        if (_maximumTargets < 1)
        {
            _maximumTargets = 1;
        }
    }
}
