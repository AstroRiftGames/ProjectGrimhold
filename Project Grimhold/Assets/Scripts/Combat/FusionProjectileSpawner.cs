using Fusion;
using UnityEngine;

/// <summary>
/// Adaptador de red Unity/Fusion para generar proyectiles de manera autoritativa.
/// Implementa <see cref="IProjectileSpawner"/> como un <see cref="NetworkBehaviour"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class FusionProjectileSpawner : NetworkBehaviour, IProjectileSpawner
{
    [Header("Configuración")]
    [SerializeField]
    private RangedAttackConfig _config;

    [Header("Componentes de Soporte")]
    [SerializeField]
    private MonoBehaviour _damageResolverSource;

    private IDamageResolver _damageResolver;

    private void Awake()
    {
        CacheDependencies();
    }

    public override void Spawned()
    {
        CacheDependencies();
    }

    private void CacheDependencies()
    {
        if (_damageResolverSource != null)
        {
            _damageResolver = _damageResolverSource as IDamageResolver;
        }
    }

    /// <summary>
    /// Intenta generar un proyectil en la red de forma autoritativa.
    /// Valida la autoridad, configuración y dependencias antes del spawn.
    /// </summary>
    public ProjectileSpawnResult Spawn(in ProjectileSpawnRequest request)
    {
        // 1. Validar autoridad de red
        if (!HasStateAuthority)
        {
            Debug.LogWarning($"[CombatTrace] Projectile spawn rejected: Lack of State Authority on {gameObject.name}.", this);
            return new ProjectileSpawnResult(false);
        }

        // 2. Validar configuración y dependencias
        if (_config == null || !_config.ProjectilePrefab.IsValid)
        {
            Debug.LogError($"[CombatTrace] Projectile spawn rejected: Missing or invalid RangedAttackConfig/ProjectilePrefab.", this);
            return new ProjectileSpawnResult(false);
        }

        if (_damageResolver == null)
        {
            Debug.LogError($"[CombatTrace] Projectile spawn rejected: Missing IDamageResolver dependency.", this);
            return new ProjectileSpawnResult(false);
        }

        // 3. Validar valores de la solicitud
        if (request.Direction.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning($"[CombatTrace] Projectile spawn rejected: Invalid direction.", this);
            return new ProjectileSpawnResult(false);
        }

        bool projectileInitialized = false;
        ProjectileSpawnRequest localRequest = request;

        Debug.Log($"[CombatTrace] Projectile spawn requested. OwnerId: {request.OwnerId}, Origin: {request.Origin}, Direction: {request.Direction}", this);

        // 4. Invocar el spawn de red autoritativo con inicialización previa
        NetworkSpawnStatus spawnStatus = Runner.TrySpawn(
            _config.ProjectilePrefab,
            out NetworkObject spawnedObject,
            request.Origin,
            rotation: null,
            inputAuthority: null,
            onBeforeSpawned: (_, networkObject) =>
            {
                if (networkObject.TryGetComponent(out NetworkProjectile projectile))
                {
                    projectile.InitializeNetworkState(in localRequest, _config.ImpactLayerMask.value);
                    projectile.SetRuntimeDependencies(_damageResolver);
                    projectileInitialized = true;
                    Debug.Log($"[CombatTrace] Projectile inlined onBeforeSpawned initialized.", networkObject);
                }
                else
                {
                    Debug.LogError($"{nameof(FusionProjectileSpawner)}: Spawned object does not contain a {nameof(NetworkProjectile)} component.", networkObject);
                }
            }
        );

        Debug.Log($"[CombatTrace] TrySpawn status: {spawnStatus}, spawnedObject: {(spawnedObject != null ? spawnedObject.name : "null")}, initialized: {projectileInitialized}", this);

        // 5. Resolver fallos de inicialización o spawn
        if (!projectileInitialized)
        {
            if (spawnedObject != null)
            {
                Runner.Despawn(spawnedObject);
            }
            return new ProjectileSpawnResult(false);
        }

        bool spawnSucceeded = spawnStatus == NetworkSpawnStatus.Spawned;
        return new ProjectileSpawnResult(spawnSucceeded);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheDependencies();
    }
#endif
}
