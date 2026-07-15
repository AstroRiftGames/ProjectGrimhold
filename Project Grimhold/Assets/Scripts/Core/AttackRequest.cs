using UnityEngine;

/// <summary>
/// Contiene exclusivamente información runtime del intento de ataque.
/// Las reglas de daño, rango, cooldown, velocidad, etc., pertenecen a la
/// configuración de la estrategia, no al request.
/// </summary>
public readonly struct AttackRequest
{
    public EntityId AttackerId { get; }
    public Vector2 Origin { get; }
    public Vector2 Direction { get; }
    public int SimulationTick { get; }

    public AttackRequest(
        EntityId attackerId,
        Vector2 origin,
        Vector2 direction,
        int simulationTick)
    {
        AttackerId = attackerId;
        Origin = origin;
        Direction = direction.sqrMagnitude > 0f
            ? direction.normalized
            : Vector2.zero;
        SimulationTick = simulationTick;
    }
}
