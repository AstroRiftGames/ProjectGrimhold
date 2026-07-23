#if UNITY_INCLUDE_TESTS
using Fusion;
using UnityEngine;

/// <summary>
/// Applies one fatal enemy hit from Fusion simulation so PlayMode tests exercise
/// the same authoritative timing contract as gameplay damage.
/// </summary>
public sealed class EnemyFatalDamageSimulationDriver : SimulationBehaviour
{
    public EnemyCharacter Target { get; set; }
    public bool IsRequested { get; set; }
    public DamageResult LastResult { get; private set; }

    public override void FixedUpdateNetwork()
    {
        if (!IsRequested || Target == null)
        {
            return;
        }

        IsRequested = false;
        LastResult = Target.ApplyDamage(new DamageRequest(
            new EntityId(int.MaxValue),
            Target.Id,
            1000f,
            DamageType.TrueDamage,
            Vector2.down,
            Target.transform.position,
            Runner.Tick));
    }
}
#endif
