using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one reusable occupied or empty raid-inventory slot.
/// </summary>
[DisallowMultipleComponent]
public sealed class RaidInventorySlotView : MonoBehaviour
{
    [SerializeField]
    private Image _icon;

    [SerializeField]
    private TMP_Text _nameText;

    [SerializeField]
    private TMP_Text _amountText;

    public void Present(in RaidInventorySlotData data)
    {
        if (!data.IsOccupied)
        {
            Clear();
            return;
        }

        if (_icon != null)
        {
            _icon.sprite = data.Icon;
            _icon.enabled = data.Icon != null;
        }

        if (_nameText != null)
        {
            _nameText.text = data.DisplayName;
        }

        if (_amountText != null)
        {
            _amountText.text = $"× {data.Amount}";
        }
    }

    public void Clear()
    {
        if (_icon != null)
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }

        if (_nameText != null)
        {
            _nameText.text = string.Empty;
        }

        if (_amountText != null)
        {
            _amountText.text = string.Empty;
        }
    }
}
