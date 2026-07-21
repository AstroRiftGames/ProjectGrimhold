/// <summary>
/// Contract for extensible enemy state machine states.
/// Implementations handle entering, executing network simulation ticks, and exiting states.
/// </summary>
public interface IEnemyState
{
    /// <summary>
    /// The unique state type identifier.
    /// </summary>
    EnemyStateType Type { get; }

    /// <summary>
    /// Called when the enemy transitions into this state.
    /// </summary>
    /// <param name="fsm">The enemy state machine context.</param>
    void Enter(EnemyFSM fsm);

    /// <summary>
    /// Called during network simulation ticks (FixedUpdateNetwork) on State Authority.
    /// </summary>
    /// <param name="fsm">The enemy state machine context.</param>
    void FixedUpdateNetwork(EnemyFSM fsm);

    /// <summary>
    /// Called when the enemy transitions out of this state.
    /// </summary>
    /// <param name="fsm">The enemy state machine context.</param>
    void Exit(EnemyFSM fsm);
}
