using UnityEngine;

/// <summary>
/// Clase base abstracta inmutable para configuraciones de ataque (melee, ranged, etc.).
/// </summary>
public abstract class AttackConfig : ScriptableObject
{
    [SerializeField, Min(0f)]
    private float _damage = 10f;

    [SerializeField]
    private DamageType _damageType = DamageType.Physical;

    [SerializeField, Min(0f)]
    private float _cooldownSeconds = 0.5f;

    [SerializeField]
    private AttackInputMode _inputMode = AttackInputMode.Press;

    public float Damage => _damage;
    public DamageType DamageType => _damageType;
    public float CooldownSeconds => _cooldownSeconds;
    public AttackInputMode InputMode => _inputMode;

    /// <summary>
    /// Intenta validar si la configuración actual es válida.
    /// </summary>
    /// <param name="error">Mensaje descriptivo del primer error encontrado.</param>
    /// <returns>True si la configuración es totalmente válida, de lo contrario False.</returns>
    public abstract bool TryValidate(out string error);

    /// <summary>
    /// Realiza validaciones comunes para todos los tipos de ataque.
    /// </summary>
    protected bool TryValidateCommon(out string error)
    {
        if (_damage < 0f)
        {
            error = $"{nameof(Damage)} must be greater than or equal to zero (current: {_damage}).";
            return false;
        }

        if (_cooldownSeconds < 0f)
        {
            error = $"{nameof(CooldownSeconds)} must be greater than or equal to zero (current: {_cooldownSeconds}).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    protected virtual void OnValidate()
    {
        if (_damage < 0f)
        {
            _damage = 0f;
        }

        if (_cooldownSeconds < 0f)
        {
            _cooldownSeconds = 0f;
        }
    }
}
