using Fusion;
using UnityEngine;

/// <summary>
/// Clase base abstracta para los personajes del juego (Jugadores, Enemigos y NPCs).
/// Implementa los contratos de identidad y de daño bajo un modelo de red autoritativo.
///
/// Esta clase es parte de la capa de integración de red y gameplay.
/// Gestiona la salud, el ciclo de vida y la muerte de la entidad en red.
/// </summary>
[DisallowMultipleComponent]
public abstract class CharacterBase : NetworkBehaviour, ICharacter, IDamageable
{
    [Header("Configuración de Salud")]
    [SerializeField, Min(0.1f)]
    private float _maxHealth = 100f;

    private Collider2D[] _cachedColliders;
    private EntityRegistry _registry;
    private EntityId _registeredId;
    private bool _isRegistered;

    [Networked]
    public float Health { get; private set; }

    /// <summary>
    /// Identificador estable de entidad en el core de gameplay.
    /// Mapeado desde el identificador de red asignado por Photon Fusion.
    /// </summary>
    public new EntityId Id => new EntityId(unchecked((int)Object.Id.Raw));

    /// <summary>
    /// Indica si el personaje se encuentra con vida.
    /// </summary>
    public bool IsAlive => Health > 0f;

    /// <summary>
    /// Indica si el personaje puede recibir daño en este momento.
    /// Puede ser sobrescrito para aplicar estados de invulnerabilidad temporales.
    /// </summary>
    public virtual bool CanReceiveDamage => IsAlive;

    protected virtual void Awake()
    {
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);
    }

    /// <summary>
    /// Inicializa el estado del personaje en la red y lo registra en el EntityRegistry.
    /// </summary>
    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Health = _maxHealth;
        }

        // Registrar la entidad y sus colliders asociados para la simulación
        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry != null)
        {
            _registeredId = Id;
            _isRegistered = _registry.TryRegister(_registeredId, this, _cachedColliders);
        }
    }

    /// <summary>
    /// Remueve la entidad y sus colliders del EntityRegistry al ser despawneada.
    /// </summary>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_isRegistered && _registry != null)
        {
            _registry.Unregister(_registeredId, _cachedColliders);
            _isRegistered = false;
        }
    }

    /// <summary>
    /// Procesa y aplica una solicitud de daño sobre el personaje.
    /// Este método requiere State Authority para ejecutarse de manera autoritativa.
    /// </summary>
    /// <param name="request">La solicitud de daño detallada.</param>
    /// <returns>El resultado del daño procesado.</returns>
    public DamageResult ApplyDamage(in DamageRequest request)
    {
        // El atacante o cliente local no puede confirmar de forma autoritativa el daño.
        if (!HasStateAuthority)
        {
            return new DamageResult(Id, false, 0f, Health, false, DamageFailureReason.MissingAuthority);
        }

        if (!IsAlive)
        {
            return new DamageResult(Id, false, 0f, Health, false, DamageFailureReason.TargetDead);
        }

        if (!CanReceiveDamage)
        {
            return new DamageResult(Id, false, 0f, Health, false, DamageFailureReason.TargetUnavailable);
        }

        if (request.Amount <= 0f)
        {
            return new DamageResult(Id, false, 0f, Health, false, DamageFailureReason.InvalidAmount);
        }

        float finalDamage = CalculateMitigatedDamage(request.Amount, request.DamageType);

        float previousHealth = Health;
        Health = Mathf.Max(0f, Health - finalDamage);
        float actualDamageApplied = previousHealth - Health;

        bool isFatal = Health <= 0f;

        if (isFatal)
        {
            HandleDeath();
        }

        return new DamageResult(
            Id,
            true,
            actualDamageApplied,
            Health,
            isFatal,
            DamageFailureReason.None
        );
    }

    /// <summary>
    /// Permite calcular mitigaciones de daño específicas.
    /// Puede ser sobrescrito por clases derivadas para incorporar armadura, resistencias, etc.
    /// </summary>
    /// <param name="amount">Monto original del daño.</param>
    /// <param name="damageType">Tipo de daño de la solicitud.</param>
    /// <returns>El monto de daño final tras aplicar mitigaciones.</returns>
    protected virtual float CalculateMitigatedDamage(float amount, DamageType damageType)
    {
        return amount;
    }

    /// <summary>
    /// Método llamado cuando la salud del personaje llega a 0 en State Authority.
    /// </summary>
    protected virtual void HandleDeath()
    {
        // Comportamiento base ante la muerte del personaje
    }
}
