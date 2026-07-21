/// <summary>
/// Destination capability that validates and commits complete loot reception.
/// </summary>
public interface ILootReceiver : IEntity
{
    /// <summary>
    /// Validates reception without mutating state.
    /// </summary>
    /// <returns>
    /// <see cref="LootTransferFailureReason.None"/> only when an immediate
    /// <see cref="CommitReceive"/> can apply the complete requested amount.
    /// </returns>
    LootTransferFailureReason ValidateReceive(in LootTransferRequest request);

    /// <summary>
    /// Applies a previously validated reception without performing a second gameplay validation.
    /// The caller must invoke this synchronously after successful validation, without yielding
    /// authoritative control or allowing intervening state changes.
    /// </summary>
    void CommitReceive(in LootTransferRequest request);
}
