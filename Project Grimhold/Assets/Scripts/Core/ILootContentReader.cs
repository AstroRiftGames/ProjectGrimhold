using System.Collections.Generic;

/// <summary>
/// Read capability for obtaining a complete immutable snapshot of held loot.
/// </summary>
public interface ILootContentReader : IEntity
{
    bool TryGetLootContent(out IReadOnlyList<LootEntry> content);
}
