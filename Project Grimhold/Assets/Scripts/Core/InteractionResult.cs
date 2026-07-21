public enum InteractionFailureReason
{
    None,
    InvalidTarget,
    InteractionDisabled,
    InteractorUnavailable,
    MissingStateAuthority,
    TargetUnavailable,
    ReceiverNotFound,
    LootRejected,
    OutOfRange
}

/// <summary>
/// Result details returned after an interaction attempt.
/// </summary>
public readonly struct InteractionResult
{
    public bool Success { get; }
    public bool IsConsumed { get; }
    public InteractionFailureReason FailureReason { get; }

    private InteractionResult(bool success, bool isConsumed, InteractionFailureReason failureReason)
    {
        Success = success;
        IsConsumed = isConsumed;
        FailureReason = failureReason;
    }

    public static InteractionResult Succeeded(bool isConsumed = false)
    {
        return new InteractionResult(true, isConsumed, InteractionFailureReason.None);
    }

    public static InteractionResult Rejected(InteractionFailureReason reason)
    {
        return new InteractionResult(false, false, reason);
    }
}

