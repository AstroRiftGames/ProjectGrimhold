using Fusion;
using UnityEngine;

/// <summary>
/// Defines the common movement contract for characters (Players and Enemies),
/// exposing locomotion state required by presentation and combat layers.
/// </summary>
public interface IMovementState
{
    /// <summary>
    /// The current 2D direction vector the character is facing.
    /// </summary>
    Vector2 FacingDirection { get; }

    /// <summary>
    /// Indicates whether the character is currently moving.
    /// </summary>
    NetworkBool IsMoving { get; }

    /// <summary>
    /// Indicates whether character control is enabled.
    /// </summary>
    NetworkBool IsControlEnabled { get; }
}
