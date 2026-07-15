using Fusion;
using UnityEngine;

/// <summary>
/// Implementación concreta de IDamageResolver como NetworkBehaviour para operar autoritativamente
/// dentro de la sesión de Photon Fusion.
/// </summary>
[DisallowMultipleComponent]
public sealed class DamageResolver : NetworkBehaviour, IDamageResolver
{
    private EntityRegistry _registry;

    public override void Spawned()
    {
        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry == null)
        {
            Debug.LogError($"{nameof(DamageResolver)}: EntityRegistry component was not found on the NetworkRunner GameObject.", this);
        }
    }

    /// <summary>
    /// Resuelve una solicitud de daño localizando la entidad, aplicando validaciones generales
    /// y delegando la aplicación real a la entidad de manera autoritativa.
    /// </summary>
    public DamageResult Resolve(in DamageRequest request)
    {
        // 1. Validar self-damage
        if (request.AttackerId == request.TargetId)
        {
            return new DamageResult(
                request.TargetId,
                false,
                0f,
                0f,
                false,
                DamageFailureReason.SelfDamageRejected
            );
        }

        if (_registry == null)
        {
            return new DamageResult(
                request.TargetId,
                false,
                0f,
                0f,
                false,
                DamageFailureReason.TargetUnavailable
            );
        }

        // 2. Localizar el objetivo
        if (!_registry.TryGetDamageable(request.TargetId, out IDamageable target))
        {
            return new DamageResult(
                request.TargetId,
                false,
                0f,
                0f,
                false,
                DamageFailureReason.InvalidTarget
            );
        }

        // 3. Comprobar que pueda recibir daño
        if (!target.CanReceiveDamage)
        {
            return new DamageResult(
                request.TargetId,
                false,
                0f,
                0f,
                false,
                DamageFailureReason.TargetUnavailable
            );
        }

        // 4. Delegar la aplicación del daño e incorporar validaciones de autoridad dentro de IDamageable
        return target.ApplyDamage(request);
    }
}
