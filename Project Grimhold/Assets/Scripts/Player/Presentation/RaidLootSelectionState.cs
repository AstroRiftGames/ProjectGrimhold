using System.Collections.Generic;

/// <summary>
/// Owns only the local selected loot identifier and reconciles it with authoritative snapshots.
/// </summary>
public sealed class RaidLootSelectionState
{
    public LootId SelectedLootId { get; private set; }
    public bool HasSelection => SelectedLootId.IsValid;

    public bool TrySelect(LootId lootId, IReadOnlyList<LootEntry> occupiedEntries)
    {
        if (!Contains(occupiedEntries, lootId))
        {
            return false;
        }

        SelectedLootId = lootId;
        return true;
    }

    public void Reconcile(IReadOnlyList<LootEntry> occupiedEntries)
    {
        if (HasSelection && !Contains(occupiedEntries, SelectedLootId))
        {
            Clear();
        }
    }

    public void Clear()
    {
        SelectedLootId = default;
    }

    private static bool Contains(IReadOnlyList<LootEntry> entries, LootId lootId)
    {
        if (!lootId.IsValid || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].IsValid && entries[i].LootId == lootId)
            {
                return true;
            }
        }

        return false;
    }
}
