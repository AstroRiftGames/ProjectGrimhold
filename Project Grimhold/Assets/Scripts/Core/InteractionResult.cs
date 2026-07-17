/// <summary>
/// Result details returned after an interaction attempt.
/// </summary>
public readonly struct InteractionResult
{
    public bool Success { get; }
    public bool IsConsumed { get; }
    public string ResultData { get; }

    public InteractionResult(bool success, bool isConsumed, string resultData)
    {
        Success = success;
        IsConsumed = isConsumed;
        ResultData = resultData;
    }
}
