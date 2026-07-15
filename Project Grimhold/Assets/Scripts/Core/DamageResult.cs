/// <summary>
/// Contiene el resultado tras procesar o aplicar una solicitud de daño.
/// </summary>
public readonly struct DamageResult
{
    public EntityId TargetId { get; }
    public bool IsApplied { get; }
    public float AppliedDamage { get; }
    public float RemainingHealth { get; }
    public bool IsFatal { get; }
    public DamageFailureReason FailureReason { get; }

    public DamageResult(
        EntityId targetId,
        bool isApplied,
        float appliedDamage,
        float remainingHealth,
        bool isFatal,
        DamageFailureReason failureReason)
    {
        TargetId = targetId;
        IsApplied = isApplied;
        AppliedDamage = appliedDamage;
        RemainingHealth = remainingHealth;
        IsFatal = isFatal;
        FailureReason = failureReason;
    }
}
