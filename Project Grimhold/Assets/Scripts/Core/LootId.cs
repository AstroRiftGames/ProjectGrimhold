using System;

/// <summary>
/// Small, stable, and comparable loot identifier.
/// </summary>
public readonly struct LootId : IEquatable<LootId>
{
    public string Value { get; }

    /// <summary>
    /// Indicates whether this identifier contains a usable domain value.
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    public LootId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("LootId value cannot be null or empty.", nameof(value));
        }
        Value = value;
    }

    public bool Equals(LootId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object obj) => obj is LootId other && Equals(other);

    public override int GetHashCode() => Value != null ? Value.GetHashCode(StringComparison.Ordinal) : 0;

    public override string ToString() => Value ?? string.Empty;

    public static bool operator ==(LootId left, LootId right) => left.Equals(right);
    public static bool operator !=(LootId left, LootId right) => !left.Equals(right);
}
