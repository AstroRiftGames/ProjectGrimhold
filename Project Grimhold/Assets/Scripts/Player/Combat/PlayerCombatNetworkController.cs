using System;
using Fusion;
using UnityEngine;

/// <summary>
/// Componente de red responsable de procesar la intención de ataque del jugador
/// y delegar la ejecución a la implementación de ataque activa.
///
/// Este controlador opera con cualquier estrategia que implemente el contrato
/// <see cref="IAttack"/>, aislando la simulación de gameplay de la presentación visual.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerCombatNetworkController : NetworkBehaviour
{
    [Header("Dependencias")]
    [SerializeField]
    private MonoBehaviour _characterSource;

    [SerializeField]
    private Transform _attackOrigin;

    [SerializeField]
    private MonoBehaviour _activeAttackSource;

    private ICharacter _character;
    private IAttack _activeAttack;
    private bool _dependenciesValid;
    private int _lastObservedSequence;

    [Networked]
    private NetworkButtons PreviousButtons { get; set; }

    [Networked]
    private TickTimer AttackCooldown { get; set; }

    [Networked]
    public NetworkBool IsAttackEnabled { get; private set; }

    // Sincronización para la capa de presentación
    [Networked]
    private int AttackSequence { get; set; }

    [Networked]
    private Vector2 LastAttackOrigin { get; set; }

    [Networked]
    private Vector2 LastAttackDirection { get; set; }

    [Networked]
    private int LastAttackTypeValue { get; set; }

    [Networked]
    private int LastAttackTick { get; set; }

    /// <summary>
    /// Evento local emitido en Render cuando se detecta la ejecución exitosa de un ataque en la simulación.
    /// </summary>
    public event Action<AttackPerformedEvent> AttackPerformed;

    private void Awake()
    {
        CacheDependencies();
    }

    public override void Spawned()
    {
        CacheDependencies();
        _dependenciesValid = ValidateDependencies();

        // Inicializar el valor observado local con la secuencia actual de la red
        // para evitar la reproducción de eventos de ataques anteriores al spawn de este proxy.
        _lastObservedSequence = AttackSequence;

        if (HasStateAuthority)
        {
            IsAttackEnabled = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!_dependenciesValid)
        {
            return;
        }

        // Se lee el input de Fusion. Si no hay input disponible en este tick, salimos inmediatamente.
        if (!GetInput(out PlayerNetworkInput input))
        {
            return;
        }

        NetworkButtons currentButtons = input.Buttons;
        bool attackPressed = false;

        if (_activeAttack != null)
        {
            if (_activeAttack.InputMode == AttackInputMode.Press)
            {
                attackPressed = currentButtons.WasPressed(PreviousButtons, PlayerInputButton.PrimaryAttack);
            }
            else
            {
                attackPressed = currentButtons.IsSet(PlayerInputButton.PrimaryAttack);
            }
        }

        // Se guarda el estado de botones anterior incluso si el combate está deshabilitado o en cooldown,
        // para evitar que se interprete una pulsación antigua al habilitarse el combate.
        PreviousButtons = currentButtons;

        // Solo la State Authority decide y ejecuta la estrategia de ataque autoritativa.
        if (!HasStateAuthority)
        {
            return;
        }

        if (attackPressed && IsAttackEnabled && _character.IsAlive)
        {
            TryExecuteAttack(input);
        }
    }

    public override void Render()
    {
        if (!_dependenciesValid)
        {
            return;
        }

        // Detectar cambios en la secuencia de ataque para notificar a la capa de presentación localmente
        if (AttackSequence != _lastObservedSequence)
        {
            AttackPerformedEvent performedEvent = new AttackPerformedEvent(
                _character.Id,
                (AttackType)LastAttackTypeValue,
                LastAttackOrigin,
                LastAttackDirection,
                LastAttackTick
            );

            AttackPerformed?.Invoke(performedEvent);
            _lastObservedSequence = AttackSequence;
        }
    }

    /// <summary>
    /// Intenta ejecutar el ataque activo validando cooldown, dirección y la estrategia.
    /// </summary>
    private void TryExecuteAttack(in PlayerNetworkInput input)
    {
        if (!AttackCooldown.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        Vector2 originPos = _attackOrigin.position;
        Vector2 direction = input.AimWorldPosition - originPos;

        // Rechazar direcciones inválidas con magnitud prácticamente cero
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 normalizedDirection = direction.normalized;

        AttackRequest request = new AttackRequest(
            _character.Id,
            originPos,
            normalizedDirection,
            (int)Runner.Tick
        );

        AttackResult result = _activeAttack.Execute(in request);

        if (result.WasExecuted)
        {
            float cooldownSeconds = _activeAttack.CooldownSeconds;
            if (cooldownSeconds > 0f)
            {
                AttackCooldown = TickTimer.CreateFromSeconds(Runner, cooldownSeconds);
            }
            else
            {
                AttackCooldown = TickTimer.None;
            }

            LastAttackOrigin = request.Origin;
            LastAttackDirection = request.Direction;
            LastAttackTypeValue = (int)_activeAttack.Type;
            LastAttackTick = request.SimulationTick;
            
            // Incrementar la secuencia al final para asegurar la sincronización correcta de todos los datos
            AttackSequence++;
        }
    }

    /// <summary>
    /// Cambia el estado de habilitación del ataque de manera autoritativa.
    /// </summary>
    public bool TrySetAttackEnabled(bool enabled)
    {
        if (!HasStateAuthority)
        {
            return false;
        }

        IsAttackEnabled = enabled;
        return true;
    }

    /// <summary>
    /// Cambia la estrategia de ataque activa. Requiere State Authority.
    /// </summary>
    public bool TrySetActiveAttack(MonoBehaviour attackSource)
    {
        if (!HasStateAuthority)
        {
            return false;
        }

        if (attackSource == null)
        {
            Debug.LogError($"{nameof(PlayerCombatNetworkController)}: Cannot set active attack to null.", this);
            return false;
        }

        if (attackSource is IAttack newAttack)
        {
            _activeAttackSource = attackSource;
            _activeAttack = newAttack;
            return true;
        }

        Debug.LogError($"{nameof(PlayerCombatNetworkController)}: The component {attackSource.name} does not implement {nameof(IAttack)}.", this);
        return false;
    }

    private void CacheDependencies()
    {
        if (_characterSource != null)
        {
            _character = _characterSource as ICharacter;
        }
        else
        {
            _character = GetComponent<ICharacter>();
        }

        if (_activeAttackSource != null)
        {
            _activeAttack = _activeAttackSource as IAttack;
        }

        if (_attackOrigin == null)
        {
            _attackOrigin = transform;
        }
    }

    private bool ValidateDependencies()
    {
        if (_character == null)
        {
            Debug.LogError($"{nameof(PlayerCombatNetworkController)} requires a component implementing {nameof(ICharacter)}.", this);
            return false;
        }

        if (_attackOrigin == null)
        {
            Debug.LogError($"{nameof(PlayerCombatNetworkController)} requires an assigned {nameof(_attackOrigin)} Transform.", this);
            return false;
        }

        if (_activeAttack == null)
        {
            Debug.LogError($"{nameof(PlayerCombatNetworkController)} requires a component implementing {nameof(IAttack)}.", this);
            return false;
        }

        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_characterSource == null)
        {
            _characterSource = GetComponent<MonoBehaviour>() as ICharacter != null ? GetComponent<MonoBehaviour>() : null;
        }

        CacheDependencies();
    }
#endif
}
