using System;
using Fusion;

/// <summary>
/// Abstraction for network combat controllers (Players and Enemies),
/// exposing combat events and state required by visual presentation layers.
/// </summary>
public interface ICombatController
{
    /// <summary>
    /// Event raised when an attack is successfully performed in simulation.
    /// </summary>
    event Action<AttackPerformedEvent> AttackPerformed;

    /// <summary>
    /// Indicates whether attack execution is currently enabled.
    /// </summary>
    NetworkBool IsAttackEnabled { get; }
}
