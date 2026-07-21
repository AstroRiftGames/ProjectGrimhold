using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Catálogo de definiciones de loot que permite buscar definiciones estáticas mediante su ID único.
/// </summary>
[CreateAssetMenu(fileName = "LootDefinitionCatalog", menuName = "Grimhold/Loot/Loot Definition Catalog")]
public sealed class LootDefinitionCatalog : ScriptableObject
{
    [SerializeField]
    private List<LootDefinition> _definitions = new();

    [NonSerialized]
    private Dictionary<string, LootDefinition> _definitionsById;

    [NonSerialized]
    private List<LootDefinition> _sortedDefinitions;

    [NonSerialized]
    private Dictionary<LootId, int> _indicesById;

    [NonSerialized]
    private bool _isCacheDirty = true;

    /// <summary>
    /// Gets the number of unique definitions available through the catalog.
    /// </summary>
    public int DefinitionCount
    {
        get
        {
            EnsureCache();
            return _sortedDefinitions.Count;
        }
    }

    private void OnEnable()
    {
        _isCacheDirty = true;
    }

    private void OnValidate()
    {
        _isCacheDirty = true;
    }


    /// <summary>
    /// Intenta obtener la definición de loot correspondiente al ID especificado.
    /// </summary>
    /// <param name="id">ID de la definición de loot.</param>
    /// <param name="definition">La definición encontrada, o null.</param>
    /// <returns>True si se encuentra la definición, de lo contrario false.</returns>
    public bool TryGet(string id, out LootDefinition definition)
    {
        definition = null;

        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        EnsureCache();

        return _definitionsById.TryGetValue(id, out definition);
    }

    /// <summary>
    /// Attempts to resolve the deterministic network index assigned to a loot definition.
    /// Indices are assigned by sorting valid unique IDs with ordinal comparison.
    /// </summary>
    public bool TryGetIndex(LootId lootId, out int index)
    {
        index = default;

        if (string.IsNullOrWhiteSpace(lootId.Value))
        {
            return false;
        }

        EnsureCache();
        return _indicesById.TryGetValue(lootId, out index);
    }

    /// <summary>
    /// Attempts to resolve a definition from its deterministic network index.
    /// </summary>
    public bool TryGetByIndex(int index, out LootDefinition definition)
    {
        EnsureCache();

        if (index < 0 || index >= _sortedDefinitions.Count)
        {
            definition = null;
            return false;
        }

        definition = _sortedDefinitions[index];
        return true;
    }

    private void EnsureCache()
    {
        if (_isCacheDirty || _definitionsById == null || _sortedDefinitions == null || _indicesById == null)
        {
            RebuildCache();
        }
    }

    /// <summary>
    /// Reconstruye el caché interno de definiciones por ID de forma segura.
    /// </summary>
    private void RebuildCache()
    {
        var rebuilt = new Dictionary<string, LootDefinition>(StringComparer.Ordinal);

        if (_definitions != null)
        {
            foreach (LootDefinition definition in _definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    continue;
                }

                if (rebuilt.ContainsKey(definition.Id))
                {
                    continue;
                }

                rebuilt.Add(definition.Id, definition);
            }
        }

        _definitionsById = rebuilt;

        _sortedDefinitions = new List<LootDefinition>(rebuilt.Values);
        _sortedDefinitions.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));

        _indicesById = new Dictionary<LootId, int>();
        for (int i = 0; i < _sortedDefinitions.Count; i++)
        {
            _indicesById.Add(_sortedDefinitions[i].LootId, i);
        }

        _isCacheDirty = false;
    }

    /// <summary>
    /// Valida que el catálogo de loot sea consistente y libre de errores o duplicados.
    /// </summary>
    /// <param name="error">Mensaje descriptivo del error en caso de que falle la validación.</param>
    /// <returns>True si el catálogo es válido, de lo contrario false.</returns>
    public bool TryValidate(out string error)
    {
        error = null;

        if (_definitions == null || _definitions.Count == 0)
        {
            error = "Catalog has no entries.";
            return false;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenReferences = new HashSet<LootDefinition>();

        foreach (LootDefinition definition in _definitions)
        {
            if (definition == null)
            {
                error = "Catalog contains a null definition reference.";
                return false;
            }

            if (!definition.TryValidate(out string definitionError))
            {
                error = $"Catalog contains an invalid definition: {definitionError}";
                return false;
            }

            if (!seenReferences.Add(definition))
            {
                error = $"Catalog contains duplicate reference for loot definition '{definition.Id}'.";
                return false;
            }

            if (!seenIds.Add(definition.Id))
            {
                error = $"Catalog has duplicate entry for loot ID '{definition.Id}'.";
                return false;
            }
        }

        return true;
    }
}
