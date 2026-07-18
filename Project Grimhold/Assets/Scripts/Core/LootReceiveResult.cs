public enum LootReceiveFailureReason
{
    None,
    InvalidLootId,
    InvalidAmount,
    ReceiverUnavailable,
    CapacityReached
}

public readonly struct LootReceiveResult
{
    public bool Success { get; }
    public LootReceiveFailureReason FailureReason { get; }

    private LootReceiveResult(bool success, LootReceiveFailureReason failureReason)
    {
        Success = success;
        FailureReason = failureReason;
    }

    public static LootReceiveResult Accepted()
    {
        return new LootReceiveResult(true, LootReceiveFailureReason.None);
    }

    public static LootReceiveResult Rejected(LootReceiveFailureReason reason)
    {
        return new LootReceiveResult(false, reason);
    }
}
