using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Catálogo de clases de jugador que relaciona los identificadores con sus prefabs correspondientes.
/// </summary>
[CreateAssetMenu(fileName = "PlayerClassCatalog", menuName = "Grimhold/PlayerClassCatalog")]
public sealed class PlayerClassCatalog : ScriptableObject
{
    [Serializable]
    public struct ClassEntry
    {
        public PlayerClassId ClassId;
        public NetworkPrefabRef Prefab;
    }

    [SerializeField]
    private List<ClassEntry> _entries = new();

    /// <summary>
    /// Intenta obtener el prefab correspondiente a una clase.
    /// </summary>
    public bool TryGetPrefab(PlayerClassId classId, out NetworkPrefabRef prefab)
    {
        prefab = default;

        if (_entries == null || !PlayerJoinDataCodec.IsSupported(classId))
        {
            return false;
        }

        foreach (ClassEntry entry in _entries)
        {
            if (entry.ClassId != classId)
            {
                continue;
            }

            if (!entry.Prefab.IsValid)
            {
                return false;
            }

            prefab = entry.Prefab;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Valida que el catálogo tenga una configuración consistente y libre de duplicados o referencias inválidas.
    /// </summary>
    public bool TryValidate(out string error)
    {
        error = null;
        if (_entries == null || _entries.Count == 0)
        {
            error = "Catalog has no entries.";
            return false;
        }

        var seen = new HashSet<PlayerClassId>();
        foreach (var entry in _entries)
        {
            if (entry.ClassId == PlayerClassId.None)
            {
                error = "Catalog contains an entry with class ID None.";
                return false;
            }
            if (!PlayerJoinDataCodec.IsSupported(entry.ClassId))
            {
                error = $"Catalog contains unsupported class ID: {entry.ClassId}.";
                return false;
            }
            if (!entry.Prefab.IsValid)
            {
                error = $"Catalog entry for {entry.ClassId} has an invalid prefab reference.";
                return false;
            }
            if (!seen.Add(entry.ClassId))
            {
                error = $"Catalog has duplicate entry for class ID: {entry.ClassId}.";
                return false;
            }
        }
        return true;
    }
}
