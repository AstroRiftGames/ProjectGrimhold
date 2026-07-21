using UnityEngine;

/// <summary>
/// Immutable visual data for one raid-inventory slot.
/// It is a disposable presentation value and never owns gameplay state.
/// </summary>
public readonly struct RaidInventorySlotData
{
    public bool IsOccupied { get; }
    public Sprite Icon { get; }
    public string DisplayName { get; }
    public int Amount { get; }
    public bool UsesFallback { get; }

    private RaidInventorySlotData(
        bool isOccupied,
        Sprite icon,
        string displayName,
        int amount,
        bool usesFallback)
    {
        IsOccupied = isOccupied;
        Icon = icon;
        DisplayName = displayName;
        Amount = amount;
        UsesFallback = usesFallback;
    }

    public static RaidInventorySlotData Empty => default;

    public static RaidInventorySlotData Create(
        LootEntry entry,
        LootDefinition definition,
        Sprite placeholder)
    {
        if (!entry.IsValid)
        {
            return Empty;
        }

        bool definitionMissing = definition == null;
        string displayName = definitionMissing
            ? entry.LootId.Value
            : definition.DisplayName;
        Sprite icon = definitionMissing || definition.Icon == null
            ? placeholder
            : definition.Icon;

        return new RaidInventorySlotData(
            true,
            icon,
            displayName,
            entry.Amount,
            definitionMissing || definition.Icon == null);
    }
}
