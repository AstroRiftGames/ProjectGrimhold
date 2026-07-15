/// <summary>
/// Informa si el ataque pudo ejecutarse en la simulación.
/// </summary>
public readonly struct AttackResult
{
    public bool WasExecuted { get; }
    public AttackFailureReason FailureReason { get; }

    private AttackResult(
        bool wasExecuted,
        AttackFailureReason failureReason)
    {
        WasExecuted = wasExecuted;
        FailureReason = failureReason;
    }

    /// <summary>
    /// Crea un resultado exitoso de ejecución.
    /// </summary>
    public static AttackResult Executed() =>
        new(true, AttackFailureReason.None);

    /// <summary>
    /// Crea un resultado fallido o rechazado indicando la causa.
    /// </summary>
    public static AttackResult Rejected(AttackFailureReason reason) =>
        new(false, reason);
}
