public interface IExtractionParticipant
{
    ExtractionState State { get; }

    bool IsInsideExtractionZone { get; }

    bool BeginExtraction();

    void CancelExtraction();

    bool CompleteExtraction();
}