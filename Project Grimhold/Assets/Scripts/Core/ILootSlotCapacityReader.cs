/// <summary>
/// Read capability for gameplay slot capacity and current occupancy.
/// One distinct positive loot identifier occupies one slot.
/// </summary>
public interface ILootSlotCapacityReader : IEntity
{
    int SlotCapacity { get; }
    int OccupiedSlotCount { get; }
}
