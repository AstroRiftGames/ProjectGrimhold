using UnityEngine;

/// <summary>
/// Parameters used to query interactable targets spatially.
/// </summary>
public readonly struct InteractionTargetQuery
{
    public EntityId InteractorId { get; }
    public Vector2 Origin { get; }
    public float MaximumDistance { get; }
    public LayerMask TargetLayerMask { get; }

    public InteractionTargetQuery(EntityId interactorId, Vector2 origin, float maximumDistance, LayerMask targetLayerMask)
    {
        InteractorId = interactorId;
        Origin = origin;
        MaximumDistance = maximumDistance;
        TargetLayerMask = targetLayerMask;
    }
}
