using System;
using System.Collections.Generic;

/// <summary>
/// Pure logic resolver that shares target selection between authoritative gameplay
/// and read-only local presentation.
/// </summary>
public static class InteractionResolver
{
    public delegate bool TryGetInteractableDelegate(EntityId id, out IInteractable interactable);

    /// <summary>
    /// Selects the first valid candidate without executing gameplay.
    /// </summary>
    public static bool TrySelect(
        EntityId interactorId,
        int simulationTick,
        float maxDistance,
        IReadOnlyList<InteractionTarget> candidates,
        TryGetInteractableDelegate lookup,
        out InteractionTarget selectedTarget,
        out InteractionRequest selectedRequest,
        out IInteractable selectedInteractable)
    {
        selectedTarget = default;
        selectedRequest = default;
        selectedInteractable = null;

        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        if (lookup == null) throw new ArgumentNullException(nameof(lookup));
        if (interactorId.Value == 0) return false;
        if (maxDistance <= 0f || float.IsNaN(maxDistance) || float.IsInfinity(maxDistance)) return false;

        for (int i = 0; i < candidates.Count; i++)
        {
            InteractionTarget candidate = candidates[i];

            if (candidate.TargetId == interactorId || candidate.TargetId.Value == 0) continue;
            if (candidate.Distance < 0f || float.IsNaN(candidate.Distance) || float.IsInfinity(candidate.Distance)) continue;
            if (candidate.Distance > maxDistance) continue;
            if (!lookup(candidate.TargetId, out IInteractable interactable) || interactable == null) continue;

            InteractionRequest request = new InteractionRequest(interactorId, candidate.TargetId, simulationTick);
            if (!interactable.CanInteract(request)) continue;

            selectedTarget = candidate;
            selectedRequest = request;
            selectedInteractable = interactable;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Selects and executes only the first valid interaction target.
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
        if (!TrySelect(
                interactorId,
                simulationTick,
                maxDistance,
                candidates,
                lookup,
                out _,
                out resolvedRequest,
                out IInteractable selectedInteractable))
        {
            resolvedResult = default;
            return false;
        }

        resolvedResult = selectedInteractable.Interact(resolvedRequest);
        return true;
    }
}
