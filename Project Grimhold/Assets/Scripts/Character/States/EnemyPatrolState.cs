using UnityEngine;

/// <summary>
/// Enemy state active during standard patrol locomotion.
/// Enables movement AI and monitors target acquisition.
/// </summary>
public sealed class EnemyPatrolState : IEnemyState
{
    public EnemyStateType Type => EnemyStateType.Patrol;

    public void Enter(EnemyFSM fsm)
    {
        if (fsm.MovementController != null)
        {
            fsm.MovementController.TrySetControlEnabled(true);
        }

        if (fsm.CombatController != null)
        {
            fsm.CombatController.TrySetAttackEnabled(false);
        }
    }

    public void FixedUpdateNetwork(EnemyFSM fsm)
    {
        if (!fsm.Character.IsAlive)
        {
            fsm.TransitionTo(EnemyStateType.Dead);
            return;
        }

        if (fsm.MovementController.IsAttacking)
        {
            fsm.TransitionTo(EnemyStateType.Attack);
            return;
        }

        if (fsm.MovementController.IsOnPursuit)
        {
            fsm.TransitionTo(EnemyStateType.Chase);
            return;
        }
    }

    public void Exit(EnemyFSM fsm)
    {
    }
}
