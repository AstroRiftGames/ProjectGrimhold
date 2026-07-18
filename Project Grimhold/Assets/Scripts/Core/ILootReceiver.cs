/// <summary>
/// Target receiver contract for loot pickup.
/// </summary>
public interface ILootReceiver : IEntity
{
    LootReceiveResult TryGrantLoot(in LootGrantRequest request);
}
