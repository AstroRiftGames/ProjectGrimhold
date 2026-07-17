/// <summary>
/// Request payload containing interaction context.
/// </summary>
public readonly struct InteractionRequest
{
    public EntityId InteractorId { get; }
    public EntityId TargetId { get; }
    public int SimulationTick { get; }

    public InteractionRequest(EntityId interactorId, EntityId targetId, int simulationTick)
    {
        InteractorId = interactorId;
        TargetId = targetId;
        SimulationTick = simulationTick;
    }
}
