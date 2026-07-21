/// <summary>
/// Immutable domain request to move a complete loot quantity between two entities.
/// </summary>
public readonly struct LootTransferRequest
{
    public EntityId SourceId { get; }
    public EntityId DestinationId { get; }
    public LootId LootId { get; }
    public int RequestedAmount { get; }
    public int SimulationTick { get; }

    public bool IsValid =>
        SourceId.Value != 0 &&
        DestinationId.Value != 0 &&
        LootId.IsValid &&
        RequestedAmount > 0;

    public LootTransferRequest(
        EntityId sourceId,
        EntityId destinationId,
        LootId lootId,
        int requestedAmount,
        int simulationTick)
    {
        SourceId = sourceId;
        DestinationId = destinationId;
        LootId = lootId;
        RequestedAmount = requestedAmount;
        SimulationTick = simulationTick;
    }
}
