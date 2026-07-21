using UnityEngine;

/// <summary>
/// Terminal enemy state active upon character death.
/// Disables movement and combat controllers.
/// </summary>
public sealed class EnemyDeadState : IEnemyState
{
    public EnemyStateType Type => EnemyStateType.Dead;

    public void Enter(EnemyFSM fsm)
    {
        if (fsm.MovementController != null)
        {
            fsm.MovementController.TrySetControlEnabled(false);
        }

        if (fsm.CombatController != null)
        {
            fsm.CombatController.TrySetAttackEnabled(false);
        }
    }

    public void FixedUpdateNetwork(EnemyFSM fsm)
    {
        // Terminal state; no transitions out.
    }

    public void Exit(EnemyFSM fsm)
    {
    }
}
