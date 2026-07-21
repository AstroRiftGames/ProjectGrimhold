/// <summary>
/// Immutable description of one authoritative loot delivery for local presentation.
/// Visual metadata is resolved locally from the loot catalog.
/// </summary>
public readonly struct LootGrantPresentationEvent
{
    public int Sequence { get; }
    public EntityId SourceId { get; }
    public EntityId ReceiverId { get; }
    public LootId LootId { get; }
    public int Amount { get; }
    public int SimulationTick { get; }

    public LootGrantPresentationEvent(
        int sequence,
        EntityId sourceId,
        EntityId receiverId,
        LootId lootId,
        int amount,
        int simulationTick)
    {
        Sequence = sequence;
        SourceId = sourceId;
        ReceiverId = receiverId;
        LootId = lootId;
        Amount = amount;
        SimulationTick = simulationTick;
    }
}
