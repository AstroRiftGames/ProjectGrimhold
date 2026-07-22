using System;
using UnityEngine;

/// <summary>
/// Inspector configuration for one initial stack in a network loot container.
/// It is configuration only and never becomes mutable runtime state.
/// </summary>
[Serializable]
public struct LootContainerInitialEntry
{
    [SerializeField]
    private LootDefinition _definition;

    [SerializeField, Min(1)]
    private int _amount;

    public LootDefinition Definition => _definition;
    public int Amount => _amount;

    public LootContainerInitialEntry(LootDefinition definition, int amount)
    {
        _definition = definition;
        _amount = amount;
    }
}
