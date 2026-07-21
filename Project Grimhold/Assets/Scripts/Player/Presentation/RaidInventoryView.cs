using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Owns the provisional raid-inventory visual hierarchy and reusable slot pool.
/// It renders presentation values without reading or mutating gameplay state.
/// </summary>
[DisallowMultipleComponent]
public sealed class RaidInventoryView : MonoBehaviour
{
    [SerializeField]
    private GameObject _screenRoot;

    [SerializeField]
    private RectTransform _slotContainer;

    [SerializeField]
    private RaidInventorySlotView _slotPrefab;

    [SerializeField]
    private TMP_Text _totalValueText;

    [SerializeField]
    private GameObject _unavailableRoot;

    [SerializeField]
    private Sprite _placeholderIcon;

    private readonly List<RaidInventorySlotView> _slots = new();

    public bool IsOpen => _screenRoot != null && _screenRoot.activeSelf;
    public Sprite PlaceholderIcon => _placeholderIcon;
    public int SlotCount => _slots.Count;

    public bool EnsureSlotCount(int slotCount)
    {
        if (slotCount <= 0 || _slotContainer == null || _slotPrefab == null)
        {
            return false;
        }

        while (_slots.Count < slotCount)
        {
            RaidInventorySlotView slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Clear();
            _slots.Add(slot);
        }

        while (_slots.Count > slotCount)
        {
            int lastIndex = _slots.Count - 1;
            RaidInventorySlotView slot = _slots[lastIndex];
            _slots.RemoveAt(lastIndex);
            Destroy(slot.gameObject);
        }

        return true;
    }

    public bool Present(IReadOnlyList<RaidInventorySlotData> slots, long? totalValue)
    {
        if (slots == null || slots.Count != _slots.Count)
        {
            return false;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            RaidInventorySlotData data = slots[i];
            _slots[i].Present(in data);
        }

        if (_totalValueText != null)
        {
            _totalValueText.text = totalValue.HasValue
                ? $"Valor: {totalValue.Value}"
                : "Valor: —";
        }

        SetUnavailable(false);
        return true;
    }

    public void ShowUnavailable()
    {
        ClearContent();
        SetUnavailable(true);
    }

    public void ClearContent()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].Clear();
        }

        if (_totalValueText != null)
        {
            _totalValueText.text = "Valor: —";
        }

        SetUnavailable(false);
    }

    public void SetScreenVisible(bool visible)
    {
        if (_screenRoot != null && _screenRoot.activeSelf != visible)
        {
            _screenRoot.SetActive(visible);
        }
    }

    private void SetUnavailable(bool unavailable)
    {
        if (_unavailableRoot != null && _unavailableRoot.activeSelf != unavailable)
        {
            _unavailableRoot.SetActive(unavailable);
        }
    }
}
