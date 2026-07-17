/// <summary>
/// Minimal event payload emitted to local presentation layers during Render.
/// </summary>
public readonly struct InteractionPresentationEvent
{
    public EntityId InteractorId { get; }
    public EntityId TargetId { get; }
    public int SimulationTick { get; }
    public bool Success { get; }
    public bool IsConsumed { get; }

    public InteractionPresentationEvent(
        EntityId interactorId,
        EntityId targetId,
        int simulationTick,
        bool success,
        bool isConsumed)
    {
        InteractorId = interactorId;
        TargetId = targetId;
        SimulationTick = simulationTick;
        Success = success;
        IsConsumed = isConsumed;
    }
}
