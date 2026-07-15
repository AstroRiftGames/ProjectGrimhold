/// <summary>
/// Informa si la generación del proyectil se completó exitosamente en la red.
/// </summary>
public readonly struct ProjectileSpawnResult
{
    public bool WasSpawned { get; }

    public ProjectileSpawnResult(bool wasSpawned)
    {
        WasSpawned = wasSpawned;
    }
}
