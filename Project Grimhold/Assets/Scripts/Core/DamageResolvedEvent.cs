/// <summary>
/// Contiene el resultado de un daño resuelto para notificaciones locales o de presentación.
/// </summary>
public readonly struct DamageResolvedEvent
{
    public DamageRequest Request { get; }
    public DamageResult Result { get; }

    public DamageResolvedEvent(in DamageRequest request, in DamageResult result)
    {
        Request = request;
        Result = result;
    }
}
