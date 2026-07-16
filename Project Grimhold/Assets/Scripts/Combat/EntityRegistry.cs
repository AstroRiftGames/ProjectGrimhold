using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Associative entity registry for a NetworkRunner.
/// Maps efficiently from EntityId to IDamageable and from Collider2D to EntityId
/// without incurring GetComponent calls in combat simulation loops.
/// </summary>
[DisallowMultipleComponent]
public sealed class EntityRegistry : MonoBehaviour
{
    private readonly Dictionary<EntityId, IDamageable> _entities = new();
    private readonly Dictionary<Collider2D, EntityId> _colliders = new();

    /// <summary>
    /// Attempts to register an entity and its associated colliders.
    /// </summary>
    public bool TryRegister(EntityId id, IDamageable damageable, IReadOnlyList<Collider2D> colliders)
    {
        if (damageable == null)
        {
            return false;
        }

        _entities[id] = damageable;

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
    /// Removes an entity and its associated colliders from the registry.
    /// </summary>
    public void Unregister(EntityId id, IReadOnlyList<Collider2D> colliders)
    {
        _entities.Remove(id);

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider2D col = colliders[i];
                if (col != null)
                {
                    _colliders.Remove(col);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to retrieve a damageable entity by its EntityId.
    /// </summary>
    public bool TryGetDamageable(EntityId id, out IDamageable damageable)
    {
        return _entities.TryGetValue(id, out damageable);
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
