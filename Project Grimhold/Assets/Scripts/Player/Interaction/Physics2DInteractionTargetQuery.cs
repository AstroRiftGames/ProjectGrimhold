using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Implementation of IInteractionTargetQuery based on Physics2D and EntityRegistry.
/// </summary>
[DisallowMultipleComponent]
public sealed class Physics2DInteractionTargetQuery : NetworkBehaviour, IInteractionTargetQuery
{
    [Header("Performance Configuration")]
    [SerializeField]
    private int _colliderBufferSize = 64;

    private Collider2D[] _colliderBuffer;
    private EntityRegistry _registry;

    private struct CandidateTarget
    {
        public EntityId Id;
        public Vector2 ClosestPoint;
        public float Distance;
    }

    private readonly List<CandidateTarget> _candidates = new();
    private readonly List<InteractionTarget> _results = new();
    private readonly Dictionary<EntityId, int> _entityIdToCandidateIndex = new();

    private void Awake()
    {
        _colliderBuffer = new Collider2D[_colliderBufferSize];
    }

    public override void Spawned()
    {
        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry == null)
        {
            Debug.LogError($"{nameof(Physics2DInteractionTargetQuery)}: EntityRegistry component was not found on the NetworkRunner GameObject.", this);
        }
    }

    /// <summary>
    /// Finds interactable candidates around the query origin.
    /// Deduplicates by EntityId, resolves via EntityRegistry, and sorts by distance/ID.
    /// </summary>
    public IReadOnlyList<InteractionTarget> FindTargets(in InteractionTargetQuery query)
    {
        _results.Clear();

        if (_registry == null)
        {
            return _results;
        }

        // Filter configuration
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.SetLayerMask(query.TargetLayerMask);
        filter.useTriggers = true;

        int hitCount = Physics2D.OverlapCircle(
            query.Origin,
            query.MaximumDistance,
            filter,
            _colliderBuffer
        );

        _candidates.Clear();
        _entityIdToCandidateIndex.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = _colliderBuffer[i];
            if (col == null)
            {
                continue;
            }

            // Retrieve EntityId from registry
            if (!_registry.TryGetEntityId(col, out EntityId targetId))
            {
                continue;
            }

            // Exclude interactor
            if (targetId == query.InteractorId)
            {
                continue;
            }

            // Exclude default invalid IDs
            if (targetId.Value == 0)
            {
                continue;
            }

            Vector2 closestPoint = col.ClosestPoint(query.Origin);
            float distance = Vector2.Distance(closestPoint, query.Origin);

            // Deduplicate: keep the closest hit per EntityId
            if (_entityIdToCandidateIndex.TryGetValue(targetId, out int existingIndex))
            {
                if (distance < _candidates[existingIndex].Distance)
                {
                    _candidates[existingIndex] = new CandidateTarget
                    {
                        Id = targetId,
                        ClosestPoint = closestPoint,
                        Distance = distance
                    };
                }
            }
            else
            {
                _entityIdToCandidateIndex[targetId] = _candidates.Count;
                _candidates.Add(new CandidateTarget
                {
                    Id = targetId,
                    ClosestPoint = closestPoint,
                    Distance = distance
                });
            }
        }

        // Clear collider buffer to avoid leaks
        Array.Clear(_colliderBuffer, 0, hitCount);
        _entityIdToCandidateIndex.Clear();

        if (_candidates.Count == 0)
        {
            return _results;
        }

        // Sort by distance ascending, then ID ascending as a tie-breaker
        _candidates.Sort(CompareCandidates);

        for (int i = 0; i < _candidates.Count; i++)
        {
            var candidate = _candidates[i];
            _results.Add(new InteractionTarget(candidate.Id, candidate.ClosestPoint, candidate.Distance));
        }

        return _results;
    }

    private static int CompareCandidates(CandidateTarget a, CandidateTarget b)
    {
        int distanceCompare = a.Distance.CompareTo(b.Distance);
        if (distanceCompare != 0)
        {
            return distanceCompare;
        }
        return a.Id.Value.CompareTo(b.Id.Value);
    }
}
