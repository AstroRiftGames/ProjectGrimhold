using UnityEngine;

/// <summary>
/// Local prefab configuration that opts a loot container into authoritative random initialization.
/// It owns no runtime state and performs no roll itself.
/// </summary>
[DisallowMultipleComponent]
public sealed class LootContainerRandomContentConfig : MonoBehaviour
{
    [SerializeField]
    private LootContainerContentTable _table;

    public LootContainerContentTable Table => _table;
}
