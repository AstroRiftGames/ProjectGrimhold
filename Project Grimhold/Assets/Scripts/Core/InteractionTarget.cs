using UnityEngine;

/// <summary>
/// Minimal representation of a candidate interaction target.
/// </summary>
public readonly struct InteractionTarget
{
    public EntityId TargetId { get; }
    public Vector2 ClosestPoint { get; }
    public float Distance { get; }

    public InteractionTarget(EntityId targetId, Vector2 closestPoint, float distance)
    {
        TargetId = targetId;
        ClosestPoint = closestPoint;
        Distance = distance;
    }
}
