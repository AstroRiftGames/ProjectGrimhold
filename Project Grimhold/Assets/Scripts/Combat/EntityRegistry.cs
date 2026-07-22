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
    private readonly Dictionary<EntityId, LootSourceRegistration> _lootSources = new();
    private readonly Dictionary<Collider2D, EntityId> _colliders = new();

    private readonly struct LootSourceRegistration
    {
        public ILootExtractor Extractor { get; }
        public ILootQuantityReader QuantityReader { get; }
        public Collider2D[] Colliders { get; }

        public LootSourceRegistration(
            ILootExtractor extractor,
            ILootQuantityReader quantityReader,
            Collider2D[] colliders)
        {
            Extractor = extractor;
            QuantityReader = quantityReader;
            Colliders = colliders;
        }
    }

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

        if (entity.Id != id)
        {
            return false;
        }

        if (entity is IDamageable damageableCandidate &&
            _entities.TryGetValue(id, out IDamageable existingDamageable) &&
            !ReferenceEquals(existingDamageable, damageableCandidate))
        {
            return false;
        }

        if (entity is IInteractable interactableCandidate &&
            _interactables.TryGetValue(id, out IInteractable existingInteractable) &&
            !ReferenceEquals(existingInteractable, interactableCandidate))
        {
            return false;
        }

        if (entity is ILootReceiver receiverCandidate &&
            _lootReceivers.TryGetValue(id, out ILootReceiver existingReceiver) &&
            !ReferenceEquals(existingReceiver, receiverCandidate))
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
    /// Registers only an interactable capability for an existing or future entity ID.
    /// Collider ownership and every other capability remain unchanged.
    /// </summary>
    public bool TryRegisterInteractable(EntityId id, IInteractable interactable)
    {
        if (interactable == null || id.Value == 0 || interactable.Id != id)
        {
            return false;
        }

        if (_interactables.TryGetValue(id, out IInteractable existing))
        {
            return ReferenceEquals(existing, interactable);
        }

        _interactables.Add(id, interactable);
        return true;
    }

    /// <summary>
    /// Removes only the interactable capability owned by the expected instance.
    /// Loot-source registration and collider mappings are not modified.
    /// </summary>
    public bool TryUnregisterInteractable(EntityId id, IInteractable expectedInteractable)
    {
        if (expectedInteractable == null || id.Value == 0 ||
            !_interactables.TryGetValue(id, out IInteractable existing) ||
            !ReferenceEquals(existing, expectedInteractable))
        {
            return false;
        }

        _interactables.Remove(id);
        return true;
    }

    /// <summary>
    /// Attempts to retrieve a loot receiver entity by its EntityId.
    /// </summary>
    public bool TryGetLootReceiver(EntityId id, out ILootReceiver lootReceiver)
    {
        return _lootReceivers.TryGetValue(id, out lootReceiver);
    }

    /// <summary>
    /// Registers a loot receiver mapping separate from other entities' contracts.
    /// </summary>
    public bool TryRegisterLootReceiver(EntityId id, ILootReceiver receiver)
    {
        if (receiver == null || id.Value == 0 || receiver.Id != id)
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
    /// Atomically registers a loot source's extraction, quantity and collider capabilities.
    /// All conflicts are checked before any registry map is changed.
    /// </summary>
    public bool TryRegisterLootSource(
        EntityId id,
        ILootExtractor extractor,
        ILootQuantityReader quantityReader,
        IReadOnlyList<Collider2D> colliders)
    {
        if (id.Value == 0 || extractor == null || quantityReader == null ||
            extractor.Id != id || quantityReader.Id != id)
        {
            return false;
        }

        if (_lootSources.TryGetValue(id, out LootSourceRegistration existing))
        {
            return ReferenceEquals(existing.Extractor, extractor) &&
                ReferenceEquals(existing.QuantityReader, quantityReader);
        }

        int colliderCount = colliders?.Count ?? 0;
        var copiedColliders = new Collider2D[colliderCount];
        for (int i = 0; i < colliderCount; i++)
        {
            Collider2D collider = colliders[i];
            copiedColliders[i] = collider;
            if (collider != null && _colliders.TryGetValue(collider, out EntityId existingId) && existingId != id)
            {
                return false;
            }
        }

        // Mutation starts only after every capability and collider has passed validation.
        _lootSources.Add(id, new LootSourceRegistration(extractor, quantityReader, copiedColliders));
        for (int i = 0; i < copiedColliders.Length; i++)
        {
            if (copiedColliders[i] != null)
            {
                _colliders[copiedColliders[i]] = id;
            }
        }

        return true;
    }

    /// <summary>
    /// Removes a grouped loot source only when both expected capability instances match.
    /// </summary>
    public bool TryUnregisterLootSource(
        EntityId id,
        ILootExtractor expectedExtractor,
        ILootQuantityReader expectedQuantityReader)
    {
        if (!_lootSources.TryGetValue(id, out LootSourceRegistration existing) ||
            !ReferenceEquals(existing.Extractor, expectedExtractor) ||
            !ReferenceEquals(existing.QuantityReader, expectedQuantityReader))
        {
            return false;
        }

        _lootSources.Remove(id);
        for (int i = 0; i < existing.Colliders.Length; i++)
        {
            Collider2D collider = existing.Colliders[i];
            if (collider != null && _colliders.TryGetValue(collider, out EntityId mappedId) && mappedId == id)
            {
                _colliders.Remove(collider);
            }
        }

        return true;
    }

    /// <summary>
    /// Resolves both capabilities that comprise a registered loot source.
    /// </summary>
    public bool TryGetLootSource(
        EntityId id,
        out ILootExtractor extractor,
        out ILootQuantityReader quantityReader)
    {
        if (_lootSources.TryGetValue(id, out LootSourceRegistration registration))
        {
            extractor = registration.Extractor;
            quantityReader = registration.QuantityReader;
            return true;
        }

        extractor = null;
        quantityReader = null;
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
