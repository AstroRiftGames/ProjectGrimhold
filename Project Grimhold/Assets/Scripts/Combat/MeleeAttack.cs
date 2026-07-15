using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Estrategia concreta de ataque cuerpo a cuerpo (Melee).
/// Utiliza IAttackTargetQuery para consultar objetivos físicamente y IDamageResolver para aplicar daño.
/// </summary>
[DisallowMultipleComponent]
public sealed class MeleeAttack : MonoBehaviour, IAttack
{
    [Header("Configuración")]
    [SerializeField]
    private MeleeAttackConfig _config;

    [Header("Componentes de Soporte")]
    [SerializeField]
    private MonoBehaviour _targetQuerySource;

    [SerializeField]
    private MonoBehaviour _damageResolverSource;

    private IAttackTargetQuery _targetQuery;
    private IDamageResolver _damageResolver;
    private bool _isValid;

    private readonly HashSet<EntityId> _tempProcessedIds = new();

    public AttackType Type => AttackType.Melee;
    public float CooldownSeconds => _config != null ? _config.CooldownSeconds : 0f;
    public AttackInputMode InputMode => _config != null ? _config.InputMode : AttackInputMode.Press;

    private void Awake()
    {
        if (_targetQuery == null || _damageResolver == null)
        {
            CacheDependencies();
        }
    }

    private void Start()
    {
        if (!_isValid)
        {
            _isValid = ValidateDependencies();
        }
    }

    /// <summary>
    /// Inicializa de forma explícita las dependencias para pruebas o instanciación dinámica.
    /// </summary>
    public void Initialize(MeleeAttackConfig config, IAttackTargetQuery targetQuery, IDamageResolver damageResolver)
    {
        _config = config;
        _targetQuery = targetQuery;
        _damageResolver = damageResolver;
        _isValid = true;
    }

    private void CacheDependencies()
    {
        if (_targetQuerySource != null)
        {
            _targetQuery = _targetQuerySource as IAttackTargetQuery;
        }

        if (_damageResolverSource != null)
        {
            _damageResolver = _damageResolverSource as IDamageResolver;
        }
    }

    private bool ValidateDependencies()
    {
        if (_config == null)
        {
            Debug.LogError($"{nameof(MeleeAttack)}: Missing MeleeAttackConfig on GameObject {gameObject.name}.", this);
            return false;
        }

        if (!_config.TryValidate(out string error))
        {
            Debug.LogError($"{nameof(MeleeAttack)}: Invalid configuration on GameObject {gameObject.name}. Error: {error}", this);
            return false;
        }

        if (_config.MaximumTargets <= 0)
        {
            Debug.LogError($"{nameof(MeleeAttack)}: MaximumTargets must be greater than zero on GameObject {gameObject.name}.", this);
            return false;
        }

        if (_targetQuery == null)
        {
            Debug.LogError($"{nameof(MeleeAttack)}: Target query component does not implement {nameof(IAttackTargetQuery)} on GameObject {gameObject.name}.", this);
            return false;
        }

        if (_damageResolver == null)
        {
            Debug.LogError($"{nameof(MeleeAttack)}: Damage resolver component does not implement {nameof(IDamageResolver)} on GameObject {gameObject.name}.", this);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ejecuta de manera autoritativa la estrategia del ataque melee.
    /// </summary>
    public AttackResult Execute(in AttackRequest request)
    {
        if (!_isValid)
        {
            _isValid = ValidateDependencies();
            if (!_isValid)
            {
                return AttackResult.Rejected(AttackFailureReason.MissingConfiguration);
            }
        }

        if (_config.MaximumTargets <= 0)
        {
            return AttackResult.Rejected(AttackFailureReason.MissingConfiguration);
        }

        // Validar dirección de ataque
        if (request.Direction.sqrMagnitude < 0.0001f)
        {
            return AttackResult.Rejected(AttackFailureReason.InvalidDirection);
        }

        // Limpiar el buffer al inicio de cada ejecución
        _tempProcessedIds.Clear();

        // 1. Construir la consulta de objetivos (con dirección normalizada)
        AttackTargetQuery targetQuery = new AttackTargetQuery(
            request.AttackerId,
            request.Origin,
            request.Direction.normalized,
            _config.Range,
            _config.Radius,
            _config.MaximumTargets,
            _config.TargetLayerMask.value
        );

        // 2. Realizar la consulta espacial
        var targets = _targetQuery.FindTargets(in targetQuery);

        // 3. Generar y delegar solicitudes de daño para cada objetivo único deduplicado
        int targetsCount = 0;
        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                // Ignorar al atacante por ID
                if (target.TargetId == request.AttackerId)
                {
                    continue;
                }

                // Deduplicar antes de aplicar el límite y antes de aplicar daño
                if (!_tempProcessedIds.Add(target.TargetId))
                {
                    continue;
                }

                // Generar solicitud de daño
                DamageRequest damageRequest = new DamageRequest(
                    request.AttackerId,
                    target.TargetId,
                    _config.Damage,
                    _config.DamageType,
                    request.Direction,
                    target.HitPoint,
                    request.SimulationTick
                );

                // No dependemos del resultado de Resolve para decidir si el ataque fue ejecutado
                _damageResolver.Resolve(in damageRequest);

                targetsCount++;
                if (targetsCount >= _config.MaximumTargets)
                {
                    break;
                }
            }
        }

        // Limpiar para no almacenar objetivos entre ejecuciones
        _tempProcessedIds.Clear();

        // Un ataque melee sin objetivos, o cuyos objetivos rechazan el daño, sigue siendo un ataque ejecutado.
        return AttackResult.Executed();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheDependencies();
    }
#endif
}
