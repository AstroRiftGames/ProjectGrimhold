using System;

/// <summary>
/// Immutable value object for one aggregated loot definition and its quantity.
/// </summary>
public readonly struct LootEntry : IEquatable<LootEntry>
{
    public LootId LootId { get; }
    public int Amount { get; }

    public bool IsValid => LootId.IsValid && Amount > 0;

    public LootEntry(LootId lootId, int amount)
    {
        LootId = lootId;
        Amount = amount;
    }

    public bool Equals(LootEntry other) => LootId.Equals(other.LootId) && Amount == other.Amount;

    public override bool Equals(object obj) => obj is LootEntry other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (LootId.GetHashCode() * 397) ^ Amount;
        }
    }

    public static bool operator ==(LootEntry left, LootEntry right) => left.Equals(right);

    public static bool operator !=(LootEntry left, LootEntry right) => !left.Equals(right);
}
