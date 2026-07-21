/// <summary>
/// Read capability for querying an aggregated quantity by loot identifier.
/// </summary>
public interface ILootQuantityReader : IEntity
{
    int GetLootAmount(LootId lootId);
}
