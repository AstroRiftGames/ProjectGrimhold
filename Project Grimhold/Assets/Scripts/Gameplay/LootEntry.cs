/// <summary>
/// Immutable read model for one aggregated loot definition in a player's incursion collection.
/// </summary>
public readonly struct LootEntry
{
    public LootId LootId { get; }
    public int Amount { get; }

    public LootEntry(LootId lootId, int amount)
    {
        LootId = lootId;
        Amount = amount;
    }
}
