using System.Collections.Generic;

/// <summary>
/// Builds a fixed-size presentation projection from a read-only loot snapshot.
/// The received order is preserved exactly and empty entries fill remaining slots.
/// </summary>
public static class RaidInventoryProjection
{
    public static bool TryBuild(
        IReadOnlyList<LootEntry> content,
        int slotCapacity,
        List<LootEntry> destination)
    {
        if (destination == null)
        {
            return false;
        }

        destination.Clear();

        if (content == null || slotCapacity <= 0 || content.Count > slotCapacity)
        {
            return false;
        }

        for (int i = 0; i < content.Count; i++)
        {
            destination.Add(content[i]);
        }

        while (destination.Count < slotCapacity)
        {
            destination.Add(default);
        }

        return true;
    }
}
