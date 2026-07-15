using UnityEngine;

/// <summary>
/// Representa un objetivo detectado durante una consulta de ataque.
/// </summary>
public readonly struct AttackTarget
{
    public EntityId TargetId { get; }
    public Vector2 HitPoint { get; }

    public AttackTarget(EntityId targetId, Vector2 hitPoint)
    {
        TargetId = targetId;
        HitPoint = hitPoint;
    }
}
