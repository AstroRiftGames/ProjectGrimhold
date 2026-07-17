using System.Collections.Generic;

/// <summary>
/// Interface for finding interactable candidates.
/// </summary>
public interface IInteractionTargetQuery
{
    /// <summary>
    /// Finds candidates matching the criteria, sorted by distance.
    /// </summary>
    IReadOnlyList<InteractionTarget> FindTargets(in InteractionTargetQuery query);
}
