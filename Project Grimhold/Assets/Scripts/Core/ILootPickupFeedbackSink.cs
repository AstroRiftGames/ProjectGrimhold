/// <summary>
/// Optional integration boundary used only by consumed world pickups to publish presentation feedback.
/// </summary>
public interface ILootPickupFeedbackSink
{
    /// <summary>
    /// Publishes feedback after a pickup reception has already committed successfully.
    /// </summary>
    void PublishPickupGrant(in LootTransferRequest request);
}
