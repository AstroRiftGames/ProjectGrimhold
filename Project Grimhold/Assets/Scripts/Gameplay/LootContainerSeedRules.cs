/// <summary>
/// Derives stable per-container seeds without consulting global random state.
/// </summary>
public static class LootContainerSeedRules
{
    /// <summary>
    /// Derives a stable 64-bit seed for one session, scene-load generation and spawn-point identity.
    /// </summary>
    public static ulong Derive(ulong sessionSeed, int sceneLoadGeneration, int spawnPointIndex)
    {
        return Derive(sessionSeed, sceneLoadGeneration, 0, spawnPointIndex);
    }

    /// <summary>
    /// Derives a stable seed with a domain discriminator so different spawn groups
    /// cannot share the same deterministic random stream for the same point index.
    /// </summary>
    public static ulong Derive(
        ulong sessionSeed,
        int sceneLoadGeneration,
        int domain,
        int spawnPointIndex)
    {
        ulong identity = ((ulong)(uint)sceneLoadGeneration << 32) | (uint)spawnPointIndex;
        ulong domainIdentity = ((ulong)(uint)domain << 32) | (uint)domain;
        return Mix(sessionSeed ^ Mix(identity) ^ Mix(domainIdentity));
    }

    private static ulong Mix(ulong value)
    {
        unchecked
        {
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
