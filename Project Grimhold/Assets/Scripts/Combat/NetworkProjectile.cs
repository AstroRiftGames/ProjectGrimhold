using Fusion;
using UnityEngine;

/// <summary>
/// Componente de red responsable de la simulación del proyectil.
/// Realiza el movimiento cinemático, la detección de colisiones mediante su propio volumen
/// y aplica daño autoritativo bajo State Authority en <see cref="FixedUpdateNetwork"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class NetworkProjectile : NetworkBehaviour
{
    [Header("Componentes Locales")]
    [SerializeField]
    private Collider2D _projectileCollider;

    [SerializeField]
    private Rigidbody2D _rigidbody;

    [Networked]
    private int OwnerEntityIdValue { get; set; }

    [Networked]
    private float Damage { get; set; }

    [Networked]
    private DamageType DamageType { get; set; }

    [Networked]
    private Vector2 Direction { get; set; }

    [Networked]
    private float Speed { get; set; }

    [Networked]
    private Vector2 SpawnPosition { get; set; }

    [Networked]
    private float MaximumRange { get; set; }

    [Networked]
    private TickTimer LifetimeTimer { get; set; }

    [Networked]
    private int ImpactLayerMaskValue { get; set; }

    [Networked]
    private NetworkBool ImpactConsumed { get; set; }

    [Networked]
    private int SpawnSimulationTick { get; set; }

    private ContactFilter2D _contactFilter;
    private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[16];
    private IDamageResolver _damageResolver;
    private EntityRegistry _registry;

    private void Awake()
    {
        CacheComponents();
    }

    public override void Spawned()
    {
        CacheComponents();
        ConfigureContactFilter();

        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry == null)
        {
            Debug.LogError($"{nameof(NetworkProjectile)}: EntityRegistry was not found on the NetworkRunner GameObject.", this);
        }

        // Ajustar escala para compensar Pixels Per Unit extremadamente alto (2048) del sprite
        transform.localScale = new Vector3(16f, 16f, 1f);

        // Ajustar el radio del CircleCollider2D en base a la escala para que su tamaño en el mundo sea el esperado (0.125 unidades)
        if (_projectileCollider is CircleCollider2D circleCol)
        {
            circleCol.radius = 0.125f / 16f;
        }

        // Asegurar que se renderice por encima del fondo
        if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.sortingOrder = 10;
        }

        // Alinear estado físico inicial y forzar sincronización de físicas en Unity
        if (_rigidbody != null)
        {
            _rigidbody.position = transform.position;
        }
        Physics2D.SyncTransforms();
    }

    private void CacheComponents()
    {
        if (_projectileCollider == null)
        {
            _projectileCollider = GetComponent<Collider2D>();
        }

        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            // Configurar Rigidbody a Kinematic para simulación controlada
            if (_rigidbody != null)
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
                _rigidbody.simulated = true;
            }
        }
    }

    private void ConfigureContactFilter()
    {
        _contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            useTriggers = true // Permitir interactuar con triggers según lógica de la máscara
        };
        _contactFilter.SetLayerMask(new LayerMask { value = ImpactLayerMaskValue });
    }

    private bool BelongsToOwner(Collider2D collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider == _projectileCollider)
        {
            return true;
        }

        if (_registry == null)
        {
            return false;
        }

        if (_registry.TryGetEntityId(collider, out EntityId entityId))
        {
            return entityId.Value == OwnerEntityIdValue;
        }

        return false;
    }

    /// <summary>
    /// Inicializa las propiedades de red del proyectil antes de completarse el spawn.
    /// Solo puede invocarse desde el callback de inicialización del spawner en la State Authority.
    /// </summary>
    public void InitializeNetworkState(in ProjectileSpawnRequest request, int impactLayerMaskValue)
    {
        OwnerEntityIdValue = request.OwnerId.Value;
        Damage = request.Damage;
        DamageType = request.DamageType;
        Direction = request.Direction;
        Speed = request.Speed;
        SpawnPosition = request.Origin;
        MaximumRange = request.MaximumRange;
        LifetimeTimer = TickTimer.CreateFromSeconds(Runner, request.LifetimeSeconds);
        ImpactLayerMaskValue = impactLayerMaskValue;
        ImpactConsumed = false;
        SpawnSimulationTick = request.SimulationTick;

        Debug.Log($"[CombatTrace] Projectile initialized. OwnerId: {OwnerEntityIdValue}, Damage: {Damage}, Lifetime: {request.LifetimeSeconds}, Tick: {SpawnSimulationTick}", this);
    }

    /// <summary>
    /// Asigna las dependencias locales requeridas en tiempo de ejecución.
    /// </summary>
    public void SetRuntimeDependencies(IDamageResolver damageResolver)
    {
        _damageResolver = damageResolver;
    }

    public override void FixedUpdateNetwork()
    {
        // Ejecutar simulación de gameplay únicamente bajo State Authority
        if (!HasStateAuthority)
        {
            return;
        }

        // 1. Validar expiración por lifetime
        if (LifetimeTimer.ExpiredOrNotRunning(Runner))
        {
            Debug.Log($"[CombatTrace] Projectile despawned: Lifetime expired.", this);
            Runner.Despawn(Object);
            return;
        }

        // 2. Calcular distancia acumulada y rango disponible
        Vector2 currentPosition = _rigidbody.position;
        float traveledDistance = Vector2.Distance(SpawnPosition, currentPosition);
        float remainingRange = MaximumRange - traveledDistance;

        if (remainingRange <= 0f)
        {
            Debug.Log($"[CombatTrace] Projectile despawned: Range limit exceeded (traveled: {traveledDistance}, max: {MaximumRange}).", this);
            Runner.Despawn(Object);
            return;
        }

        // 3. Determinar distancia a avanzar en este tick
        float requestedDistance = Speed * Runner.DeltaTime;
        bool reachesMaximumRange = requestedDistance >= remainingRange;
        float travelDistance = Mathf.Min(requestedDistance, remainingRange);

        // 4. Realizar el cast utilizando el volumen real del proyectil
        Physics2D.SyncTransforms();

        int hitCount = _projectileCollider.Cast(
            Direction,
            _contactFilter,
            _hitBuffer,
            travelDistance
        );

        RaycastHit2D selectHit = default;
        bool foundValidHit = false;

        // 5. Filtrar el primer impacto que no sea del propietario
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _hitBuffer[i];
            if (hit.collider == null)
            {
                continue;
            }

            if (BelongsToOwner(hit.collider))
            {
                continue;
            }

            selectHit = hit;
            foundValidHit = true;
            break;
        }

        // Limpiar el buffer localmente
        System.Array.Clear(_hitBuffer, 0, hitCount);

        // 6. Procesar el impacto o mover el proyectil
        if (foundValidHit)
        {
            if (!ImpactConsumed)
            {
                ImpactConsumed = true;

                // Mover el proyectil al punto de colisión exacto respetando el volumen
                Vector2 impactPosition = currentPosition + Direction * selectHit.distance;
                _rigidbody.position = impactPosition;
                transform.position = impactPosition;

                Debug.Log($"[CombatTrace] Projectile despawned: Impact resolved on collider {selectHit.collider.name}. Distance: {selectHit.distance}, HitPoint: {selectHit.point}", this);

                // Intentar aplicar daño si es una entidad dañable
                EntityId hitEntityId = default;
                bool isEntity = _registry != null && _registry.TryGetEntityId(selectHit.collider, out hitEntityId);

                if (isEntity && _damageResolver != null)
                {
                    DamageRequest damageRequest = new DamageRequest(
                        new EntityId(OwnerEntityIdValue),
                        hitEntityId,
                        Damage,
                        DamageType,
                        Direction,
                        selectHit.point,
                        Runner.Tick
                    );

                    _damageResolver.Resolve(in damageRequest);
                }

                Runner.Despawn(Object);
            }
        }
        else
        {
            // Movimiento normal sin colisiones detectadas
            Vector2 nextPosition = currentPosition + Direction * travelDistance;
            _rigidbody.position = nextPosition;
            transform.position = nextPosition;

            // Despawnear si se alcanzó el límite exacto del alcance en este tick
            if (reachesMaximumRange)
            {
                Debug.Log($"[CombatTrace] Projectile despawned: Reached exact maximum range.", this);
                Runner.Despawn(Object);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheComponents();
    }
#endif
}
