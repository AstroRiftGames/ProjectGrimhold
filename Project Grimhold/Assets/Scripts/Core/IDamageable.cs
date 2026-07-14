/// <summary>
/// Interfaz para cualquier entidad que pueda recibir daño.
/// </summary>
public interface IDamageable : IEntity
{
    bool CanReceiveDamage { get; }
    DamageResult ApplyDamage(in DamageRequest request);
}