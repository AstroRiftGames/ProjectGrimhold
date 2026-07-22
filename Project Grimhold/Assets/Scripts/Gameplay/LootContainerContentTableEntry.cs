using System;
using UnityEngine;

/// <summary>
/// Stable configuration for one weighted candidate in a loot-container content table.
/// </summary>
[Serializable]
public struct LootContainerContentTableEntry
{
    [SerializeField]
    private LootDefinition _definition;

    [SerializeField]
    private ulong _weight;

    [SerializeField]
    private int _minimumAmount;

    [SerializeField]
    private int _maximumAmount;

    public LootDefinition Definition => _definition;
    public ulong Weight => _weight;
    public int MinimumAmount => _minimumAmount;
    public int MaximumAmount => _maximumAmount;

    public LootContainerContentTableEntry(
        LootDefinition definition,
        ulong weight,
        int minimumAmount,
        int maximumAmount)
    {
        _definition = definition;
        _weight = weight;
        _minimumAmount = minimumAmount;
        _maximumAmount = maximumAmount;
    }
}
