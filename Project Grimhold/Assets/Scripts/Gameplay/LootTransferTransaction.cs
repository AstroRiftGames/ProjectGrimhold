using System;

/// <summary>
/// Executes an atomic, prevalidated full-stack transfer between two loot capabilities.
/// Resolution, range checks, presentation and callbacks remain outside this transaction.
/// </summary>
public static class LootTransferTransaction
{
    /// <summary>
    /// Validates both endpoints and commits them in extraction-then-reception order.
    /// Contract exceptions raised by commits are intentionally not converted to gameplay rejections.
    /// </summary>
    public static LootTransferResult Execute(
        ILootExtractor source,
        ILootReceiver destination,
        in LootTransferRequest request)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        LootTransferFailureReason failure = source.ValidateExtraction(request);
        if (failure != LootTransferFailureReason.None)
        {
            return LootTransferResult.Rejected(failure);
        }

        failure = destination.ValidateReceive(request);
        if (failure != LootTransferFailureReason.None)
        {
            return LootTransferResult.Rejected(failure);
        }

        source.CommitExtraction(request);
        destination.CommitReceive(request);
        return LootTransferResult.Succeeded(request);
    }
}
