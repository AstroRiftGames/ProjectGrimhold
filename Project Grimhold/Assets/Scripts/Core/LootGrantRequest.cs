/// <summary>
/// Immutable details of a request to grant loot.
/// </summary>
public readonly struct LootGrantRequest
{
    public EntityId SourceId { get; }
    public EntityId ReceiverId { get; }
    public LootId LootId { get; }
    public int Amount { get; }
    public int SimulationTick { get; }

    public LootGrantRequest(
        EntityId sourceId,
        EntityId receiverId,
        LootId lootId,
        int amount,
        int simulationTick)
    {
        SourceId = sourceId;
        ReceiverId = receiverId;
        LootId = lootId;
        Amount = amount;
        SimulationTick = simulationTick;
    }
}
