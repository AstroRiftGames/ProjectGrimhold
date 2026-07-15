using UnityEngine;

/// <summary>
/// Contiene los parámetros necesarios para realizar una consulta de objetivos.
/// </summary>
public readonly struct AttackTargetQuery
{
    public EntityId AttackerId { get; }
    public Vector2 Origin { get; }
    public Vector2 Direction { get; }
    public float Range { get; }
    public float Radius { get; }
    public int MaximumTargets { get; }
    public int TargetLayerMask { get; }

    public AttackTargetQuery(
        EntityId attackerId,
        Vector2 origin,
        Vector2 direction,
        float range,
        float radius,
        int maximumTargets,
        int targetLayerMask)
    {
        AttackerId = attackerId;
        Origin = origin;
        Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
        Range = range;
        Radius = radius;
        MaximumTargets = maximumTargets;
        TargetLayerMask = targetLayerMask;
    }
}
