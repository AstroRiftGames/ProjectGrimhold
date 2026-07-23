using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Associative entity registry for a NetworkRunner.
/// Maps efficiently from EntityId to IDamageable, IInteractable, and from Collider2D to EntityId
/// without incurring GetComponent calls in simulation loops.
/// </summary>
[DisallowMultipleComponent]
public sealed class EntityRegistry : MonoBehaviour
{
    private readonly Dictionary<EntityId, IDamageable> _entities = new();
    private readonly Dictionary<EntityId, IInteractable> _interactables = new();
    private readonly Dictionary<EntityId, ILootReceiver> _lootReceivers = new();
    private readonly Dictionary<Collider2D, EntityId> _colliders = new();
    private readonly Dictionary<EntityId, IEntity> _registeredEntities = new();
    private readonly Dictionary<EntityId, IExtractionParticipant> _extractionParticipants = new();

    /// <summary>
    /// Attempts to register an entity and its associated colliders.
    /// Backward-compatible wrapper for damageable-only registrations.
    /// </summary>
    public bool TryRegister(EntityId id, IDamageable damageable, IReadOnlyList<Collider2D> colliders)
    {
        return TryRegisterEntity(id, damageable, colliders);
    }

    /// <summary>
    /// Removes an entity and its associated colliders from the registry.
    /// Backward-compatible wrapper for damageable-only unregistrations.
    /// </summary>
    public void Unregister(EntityId id, IReadOnlyList<Collider2D> colliders)
    {
        if (_entities.TryGetValue(id, out var damageable))
        {
            TryUnregisterEntity(id, damageable);
        }
    }

    /// <summary>
    /// Registers any IEntity (which might implement IDamageable, IInteractable, or both) and its colliders.
    /// </summary>
    public bool TryRegisterEntity(EntityId id, IEntity entity, IReadOnlyList<Collider2D> colliders)
    {
        if (entity == null)
        {
            return false;
        }

        if (id.Value == 0)
        {
            return false;
        }

        if (entity.ID != id)
        {
            return false;
        }

        // Validate colliders are not registered to someone else
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider2D col = colliders[i];
                if (col != null && _colliders.TryGetValue(col, out var existingId) && existingId != id)
                {
                    return false;
                }
            }
        }

        _registeredEntities[id] = entity;

        // Register contracts
        if (entity is IDamageable damageable)
        {
            _entities[id] = damageable;
        }

        if (entity is IInteractable interactable)
        {
            _interactables[id] = interactable;
        }

        if (entity is ILootReceiver lootReceiver)
        {
            _lootReceivers[id] = lootReceiver;
        }

        if (entity is IExtractionParticipant participant)
        {
            _extractionParticipants[id] = participant;
        }

        // Register colliders
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider2D col = colliders[i];
                if (col != null)
                {
                    _colliders[col] = id;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Unregisters an entity, clearing its contracts and colliders.
    /// </summary>
    public bool TryUnregisterEntity(EntityId id, IEntity expectedEntity)
    {
        if (expectedEntity == null)
        {
            return false;
        }

        // Check damageable mapping
        bool hasDamageable = _entities.TryGetValue(id, out var damageable);
        bool hasInteractable = _interactables.TryGetValue(id, out var interactable);
        bool hasLootReceiver = _lootReceivers.TryGetValue(id, out var lootReceiver);

        if (hasDamageable && damageable != expectedEntity)
        {
            return false;
        }
        if (hasInteractable && interactable != expectedEntity)
        {
            return false;
        }
        if (hasLootReceiver && lootReceiver != expectedEntity)
        {
            return false;
        }
        if (!hasDamageable && !hasInteractable && !hasLootReceiver)
        {
            return false;
        }

        // Remove contracts
        _extractionParticipants.Remove(id);
        _entities.Remove(id);
        _interactables.Remove(id);
        _lootReceivers.Remove(id);

        // Remove colliders associated with this ID
        List<Collider2D> keysToRemove = new List<Collider2D>();
        foreach (var pair in _colliders)
        {
            if (pair.Value == id)
            {
                keysToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            _colliders.Remove(keysToRemove[i]);
        }

        return true;
    }

    /// <summary>
    /// Attempts to retrieve a damageable entity by its EntityId.
    /// </summary>
    public bool TryGetDamageable(EntityId id, out IDamageable damageable)
    {
        return _entities.TryGetValue(id, out damageable);
    }

    /// <summary>
    /// Attempts to retrieve an interactable entity by its EntityId.
    /// </summary>
    public bool TryGetInteractable(EntityId id, out IInteractable interactable)
    {
        return _interactables.TryGetValue(id, out interactable);
    }

    /// <summary>
    /// Attempts to retrieve a loot receiver entity by its EntityId.
    /// </summary>
    public bool TryGetLootReceiver(EntityId id, out ILootReceiver lootReceiver)
    {
        return _lootReceivers.TryGetValue(id, out lootReceiver);
    }

    /// <summary>
    /// Attempts to retrieve any registered entity by its EntityId.
    /// </summary>
    public bool TryGetEntity(EntityId id, out IEntity entity)
    {
        return _registeredEntities.TryGetValue(id, out entity);
    }

    /// <summary>
    /// Attempts to retrieve an extraction participant by its EntityId.
    /// </summary>
    public bool TryGetExtractionParticipant(
    EntityId id,
    out IExtractionParticipant participant)
    {
        return _extractionParticipants.TryGetValue(id, out participant);
    }

    /// <summary>
    /// Registers a loot receiver mapping separate from other entities' contracts.
    /// </summary>
    public bool TryRegisterLootReceiver(EntityId id, ILootReceiver receiver)
    {
        if (receiver == null || id.Value == 0 || receiver.ID != id)
        {
            return false;
        }

        if (_lootReceivers.TryGetValue(id, out var existing))
        {
            if (existing == receiver)
            {
                return true; // Idempotent on same instance
            }
            return false; // Rejects conflicts
        }

        _lootReceivers[id] = receiver;
        return true;
    }

    /// <summary>
    /// Unregisters a loot receiver mapping safely without removing other capacities.
    /// </summary>
    public bool TryUnregisterLootReceiver(EntityId id, ILootReceiver expectedReceiver)
    {
        if (expectedReceiver == null || id.Value == 0)
        {
            return false;
        }

        if (_lootReceivers.TryGetValue(id, out var existing))
        {
            if (existing == expectedReceiver)
            {
                _lootReceivers.Remove(id);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the EntityId that owns a given Collider2D.
    /// </summary>
    public bool TryGetEntityId(Collider2D collider, out EntityId id)
    {
        id = default;
        if (collider == null)
        {
            return false;
        }
        return _colliders.TryGetValue(collider, out id);
    }
}
