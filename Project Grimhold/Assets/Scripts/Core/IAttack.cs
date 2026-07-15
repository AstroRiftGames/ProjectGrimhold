/// <summary>
/// Encapsula una estrategia de ataque concreta (melee, ranged, etc.).
/// No contiene estado mutable durante la ejecución.
/// </summary>
public interface IAttack
{
    /// <summary>
    /// Categoría general del ataque (Melee, Ranged, etc.).
    /// </summary>
    AttackType Type { get; }

    /// <summary>
    /// Tiempo de cooldown requerido entre ataques en segundos.
    /// </summary>
    float CooldownSeconds { get; }

    /// <summary>
    /// Define el modo de pulsación esperado del botón (Press, Hold).
    /// </summary>
    AttackInputMode InputMode { get; }

    /// <summary>
    /// Ejecuta de manera autoritativa la estrategia del ataque.
    /// </summary>
    /// <param name="request">La solicitud detallada del intento de ataque.</param>
    /// <returns>El resultado de la ejecución.</returns>
    AttackResult Execute(in AttackRequest request);
}
