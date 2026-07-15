/// <summary>
/// Define cómo se activa la ejecución del ataque según la interacción con el botón.
/// </summary>
public enum AttackInputMode
{
    /// <summary>
    /// Se ejecuta una sola vez al pulsar el botón (requiere soltar y volver a pulsar para repetir).
    /// </summary>
    Press,

    /// <summary>
    /// Se ejecuta continuamente mientras el botón permanezca pulsado, respetando el cooldown.
    /// </summary>
    Hold
}
