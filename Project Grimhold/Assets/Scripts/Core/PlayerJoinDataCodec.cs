/// <summary>
/// Codec versionado responsable de serializar y deserializar los datos de conexión del jugador.
/// </summary>
public static class PlayerJoinDataCodec
{
    private const byte Version = 1;

    /// <summary>
    /// Determina si un PlayerClassId es soportado para jugar.
    /// </summary>
    public static bool IsSupported(PlayerClassId classId)
    {
        return classId == PlayerClassId.Melee || classId == PlayerClassId.Ranged;
    }

    /// <summary>
    /// Intenta codificar los datos de unión en un token de bytes.
    /// </summary>
    public static bool TryEncode(in PlayerJoinData data, out byte[] token)
    {
        if (!IsSupported(data.ClassId))
        {
            token = null;
            return false;
        }

        token = new byte[] { Version, (byte)data.ClassId };
        return true;
    }

    /// <summary>
    /// Intenta decodificar los datos de unión desde un token de bytes.
    /// </summary>
    public static bool TryDecode(byte[] token, out PlayerJoinData data)
    {
        data = new PlayerJoinData(PlayerClassId.None);

        if (token == null || token.Length != 2)
        {
            return false;
        }

        if (token[0] != Version)
        {
            return false;
        }

        PlayerClassId classId = (PlayerClassId)token[1];
        if (!IsSupported(classId))
        {
            return false;
        }

        data = new PlayerJoinData(classId);
        return true;
    }
}
