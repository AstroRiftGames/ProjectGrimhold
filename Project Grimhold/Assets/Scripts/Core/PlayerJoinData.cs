/// <summary>
/// Representa los datos inmutables de conexión del jugador al unirse a una sesión.
/// </summary>
public readonly struct PlayerJoinData
{
    /// <summary>
    /// Identificador de la clase del jugador.
    /// </summary>
    public PlayerClassId ClassId { get; }

    public PlayerJoinData(PlayerClassId classId)
    {
        ClassId = classId;
    }
}
