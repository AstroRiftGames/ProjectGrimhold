using UnityEngine;

/// <summary>
/// Representa una solicitud inmutable para generar un proyectil.
/// Contiene únicamente datos pertenecientes al core de simulación.
/// </summary>
public readonly struct ProjectileSpawnRequest
{
    public EntityId OwnerId { get; }
    public Vector2 Origin { get; }
    public Vector2 Direction { get; }
    public float Damage { get; }
    public DamageType DamageType { get; }
    public float Speed { get; }
    public float LifetimeSeconds { get; }
    public float MaximumRange { get; }
    public int SimulationTick { get; }

    public ProjectileSpawnRequest(
        EntityId ownerId,
        Vector2 origin,
        Vector2 direction,
        float damage,
        DamageType damageType,
        float speed,
        float lifetimeSeconds,
        float maximumRange,
        int simulationTick)
    {
        OwnerId = ownerId;
        Origin = origin;
        Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
        Damage = damage;
        DamageType = damageType;
        Speed = speed;
        LifetimeSeconds = lifetimeSeconds;
        MaximumRange = maximumRange;
        SimulationTick = simulationTick;
    }
}
