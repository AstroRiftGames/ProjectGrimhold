/// <summary>
/// Source capability that validates and commits complete loot extraction.
/// </summary>
public interface ILootExtractor : IEntity
{
    /// <summary>
    /// Validates extraction without mutating state.
    /// </summary>
    LootTransferFailureReason ValidateExtraction(in LootTransferRequest request);

    /// <summary>
    /// Applies a previously validated extraction without performing a second gameplay validation.
    /// The caller must invoke this synchronously after all transfer preconditions succeed.
    /// </summary>
    void CommitExtraction(in LootTransferRequest request);
}
