/// <summary>
/// Owns one non-overwritable pending request and one processed-result cache.
/// It is local adapter state and is never replicated.
/// </summary>
public sealed class LootTransferRequestState
{
    public enum Disposition
    {
        AcceptedPending,
        PendingDuplicate,
        PendingPayloadConflict,
        BusyWithDifferentSequence,
        ProcessedDuplicate,
        ProcessedPayloadConflict,
        StaleSequence
    }

    private bool _hasPending;
    private LootTransferRequestIdentity _pending;
    private bool _hasProcessed;
    private LootTransferRequestIdentity _processed;
    private LootTransferConfirmation _processedConfirmation;

    public bool HasPending => _hasPending;
    public bool HasProcessed => _hasProcessed;

    /// <summary>
    /// Classifies an incoming identity without ever replacing an existing pending request.
    /// </summary>
    public Disposition TryEnqueue(in LootTransferRequestIdentity identity, out LootTransferConfirmation cachedConfirmation)
    {
        cachedConfirmation = default;

        if (_hasPending)
        {
            if (identity.RequestSequence == _pending.RequestSequence)
            {
                return identity == _pending ? Disposition.PendingDuplicate : Disposition.PendingPayloadConflict;
            }

            return Disposition.BusyWithDifferentSequence;
        }

        if (_hasProcessed)
        {
            if (identity.RequestSequence == _processed.RequestSequence)
            {
                if (identity == _processed)
                {
                    cachedConfirmation = _processedConfirmation;
                    return Disposition.ProcessedDuplicate;
                }

                return Disposition.ProcessedPayloadConflict;
            }

            if (identity.RequestSequence < _processed.RequestSequence)
            {
                return Disposition.StaleSequence;
            }
        }

        _pending = identity;
        _hasPending = true;
        return Disposition.AcceptedPending;
    }

    /// <summary>
    /// Removes the pending request only when authoritative simulation consumes it.
    /// </summary>
    public bool TryConsume(out LootTransferRequestIdentity identity)
    {
        identity = _pending;
        if (!_hasPending)
        {
            return false;
        }

        _hasPending = false;
        return true;
    }

    /// <summary>
    /// Replaces the bounded cache with the identity and confirmation just processed.
    /// </summary>
    public void RecordProcessed(in LootTransferRequestIdentity identity, in LootTransferConfirmation confirmation)
    {
        _processed = identity;
        _processedConfirmation = confirmation;
        _hasProcessed = true;
    }

    public void Reset()
    {
        _hasPending = false;
        _pending = default;
        _hasProcessed = false;
        _processed = default;
        _processedConfirmation = default;
    }
}
