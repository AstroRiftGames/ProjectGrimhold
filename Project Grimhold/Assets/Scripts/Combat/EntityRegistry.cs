using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registro de entidades asociativas para un NetworkRunner.
/// Mapea de manera eficiente de EntityId a IDamageable y de Collider2D a EntityId
/// sin incurrir en GetComponent en loops de simulación de combate.
/// </summary>
[DisallowMultipleComponent]
public sealed class EntityRegistry : MonoBehaviour
{
    private readonly Dictionary<EntityId, IDamageable> _entities = new();
    private readonly Dictionary<Collider2D, EntityId> _colliders = new();

    /// <summary>
    /// Intenta registrar una entidad y sus colliders asociados.
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
    /// Remueve una entidad y sus colliders asociados.
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
    /// Intenta recuperar una entidad dañable mediante su EntityId.
    /// </summary>
    public bool TryGetDamageable(EntityId id, out IDamageable damageable)
    {
        return _entities.TryGetValue(id, out damageable);
    }

    /// <summary>
    /// Intenta recuperar el EntityId al cual pertenece un Collider2D.
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
