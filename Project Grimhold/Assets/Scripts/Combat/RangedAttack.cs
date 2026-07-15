using UnityEngine;

/// <summary>
/// Estrategia concreta de ataque a distancia (Ranged).
/// Traduce un <see cref="AttackRequest"/> en un <see cref="ProjectileSpawnRequest"/>
/// y delega la creación al spawner de proyectiles.
/// </summary>
[DisallowMultipleComponent]
public sealed class RangedAttack : MonoBehaviour, IAttack
{
    [Header("Configuración")]
    [SerializeField]
    private RangedAttackConfig _config;

    [Header("Componentes de Soporte")]
    [SerializeField]
    private MonoBehaviour _projectileSpawnerSource;

    private IProjectileSpawner _projectileSpawner;
    private bool _isValid;

    public AttackType Type => AttackType.Ranged;
    public float CooldownSeconds => _config != null ? _config.CooldownSeconds : 0f;
    public AttackInputMode InputMode => _config != null ? _config.InputMode : AttackInputMode.Press;

    private void Awake()
    {
        CacheDependencies();
    }

    private void Start()
    {
        _isValid = ValidateDependencies();
    }

    private void CacheDependencies()
    {
        if (_projectileSpawnerSource != null)
        {
            _projectileSpawner = _projectileSpawnerSource as IProjectileSpawner;
        }
    }

    private bool ValidateDependencies()
    {
        if (_config == null)
        {
            Debug.LogError($"{nameof(RangedAttack)}: Missing RangedAttackConfig on GameObject {gameObject.name}.", this);
            return false;
        }

        if (!_config.TryValidate(out string error))
        {
            Debug.LogError($"{nameof(RangedAttack)}: Invalid configuration on GameObject {gameObject.name}. Error: {error}", this);
            return false;
        }

        if (_projectileSpawner == null)
        {
            Debug.LogError($"{nameof(RangedAttack)}: Projectile spawner component does not implement {nameof(IProjectileSpawner)} on GameObject {gameObject.name}.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ejecuta de manera autoritativa la estrategia del ataque ranged.
    /// Solo construye la solicitud de spawn del proyectil y la delega al spawner.
    /// </summary>
    public AttackResult Execute(in AttackRequest request)
    {
        if (!_isValid)
        {
            return AttackResult.Rejected(AttackFailureReason.MissingConfiguration);
        }

        // Validar dirección de ataque
        if (request.Direction.sqrMagnitude < 0.0001f)
        {
            return AttackResult.Rejected(AttackFailureReason.InvalidDirection);
        }

        Vector2 normalizedDirection = request.Direction.normalized;
        Vector2 projectileOrigin = request.Origin + normalizedDirection * _config.ProjectileSpawnOffset;

        ProjectileSpawnRequest spawnRequest = new ProjectileSpawnRequest(
            request.AttackerId,
            projectileOrigin,
            normalizedDirection,
            _config.Damage,
            _config.DamageType,
            _config.ProjectileSpeed,
            _config.LifetimeSeconds,
            _config.MaxRange,
            request.SimulationTick
        );

        ProjectileSpawnResult spawnResult = _projectileSpawner.Spawn(in spawnRequest);

        if (spawnResult.WasSpawned)
        {
            return AttackResult.Executed();
        }

        return AttackResult.Rejected(AttackFailureReason.ExecutionFailed);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheDependencies();
    }
#endif
}
