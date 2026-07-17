using System;
using System.Collections.Generic;

/// <summary>
/// Pure logic resolver to determine the selected interactable candidate and execute the interaction.
/// </summary>
public static class InteractionResolver
{
    public delegate bool TryGetInteractableDelegate(EntityId id, out IInteractable interactable);

    /// <summary>
    /// Evaluates candidate targets in order, verifying distances, resolving IInteractables,
    /// and executing the first valid interaction.
    /// </summary>
    public static bool TryResolve(
        EntityId interactorId,
        int simulationTick,
        float maxDistance,
        IReadOnlyList<InteractionTarget> candidates,
        TryGetInteractableDelegate lookup,
        out InteractionRequest resolvedRequest,
        out InteractionResult resolvedResult)
    {
        resolvedRequest = default;
        resolvedResult = default;

        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        if (lookup == null) throw new ArgumentNullException(nameof(lookup));
        if (interactorId.Value == 0) return false;
        if (maxDistance <= 0f || float.IsNaN(maxDistance) || float.IsInfinity(maxDistance)) return false;

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];

            // 1. Exclude self
            if (candidate.TargetId == interactorId) continue;

            // 2. Validate Target IDs
            if (candidate.TargetId.Value == 0) continue;

            // 3. Validate distances defensively
            if (candidate.Distance < 0f || float.IsNaN(candidate.Distance) || float.IsInfinity(candidate.Distance)) continue;
            if (candidate.Distance > maxDistance) continue;

            // 4. Resolve interactable
            if (!lookup(candidate.TargetId, out var interactable) || interactable == null) continue;

            // 5. Create request
            var request = new InteractionRequest(interactorId, candidate.TargetId, simulationTick);

            // 6. Evaluate CanInteract
            if (!interactable.CanInteract(request)) continue;

            // 7. Authoritative execution (exactly once, stop traversal immediately)
            resolvedRequest = request;
            resolvedResult = interactable.Interact(request);
            return true;
        }

        return false;
    }
}
