using UnityEngine;

/// <summary>
/// Contiene la información de una solicitud para aplicar daño.
/// Es inmutable y no contiene estado mutable en tiempo de ejecución.
/// </summary>
public readonly struct DamageRequest
{
    public EntityId AttackerId { get; }
    public EntityId TargetId { get; }
    public float Amount { get; }
    public DamageType DamageType { get; }
    public Vector2 Direction { get; }
    public Vector2 HitPoint { get; }
    public int SimulationTick { get; }

    public DamageRequest(
        EntityId attackerId,
        EntityId targetId,
        float amount,
        DamageType damageType,
        Vector2 direction,
        Vector2 hitPoint,
        int simulationTick)
    {
        AttackerId = attackerId;
        TargetId = targetId;
        Amount = amount;
        DamageType = damageType;
        Direction = direction.normalized;
        HitPoint = hitPoint;
        SimulationTick = simulationTick;
    }
}
