using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Presents the local player's replicated incursion loot as a read-only list,
/// derived total value, and deduplicated delivery feedback.
/// </summary>
[DisallowMultipleComponent]
public sealed class LootHudPresenter : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _contentText;

    [SerializeField]
    private TMP_Text _totalValueText;

    [SerializeField]
    private GameObject _toastRoot;

    [SerializeField]
    private TMP_Text _toastText;

    [SerializeField, Min(0f)]
    private float _toastDuration = 1.5f;

    private readonly StringBuilder _contentBuilder = new(128);
    private PlayerLootReceiver _lootReceiver;
    private int _observedChangeSequence;
    private int _lastGrantSequence;
    private float _toastRemaining;
    private bool _isBound;
    private bool _reportedReadFailure;

    public void Bind(PlayerLootReceiver lootReceiver)
    {
        Unbind();

        _lootReceiver = lootReceiver;
        if (_lootReceiver == null)
        {
            ShowUnavailable();
            return;
        }

        _observedChangeSequence = _lootReceiver.LootChangeSequence;
        _lastGrantSequence = _observedChangeSequence;
        _lootReceiver.LootGranted += OnLootGranted;
        _isBound = true;
        RefreshSummary();
    }

    public void Unbind()
    {
        if (_isBound && _lootReceiver != null)
        {
            _lootReceiver.LootGranted -= OnLootGranted;
        }

        _lootReceiver = null;
        _isBound = false;
        _reportedReadFailure = false;
        HideToast();
    }

    private void Update()
    {
        if (!_isBound || _lootReceiver == null)
        {
            return;
        }

        int currentSequence = _lootReceiver.LootChangeSequence;
        if (currentSequence != _observedChangeSequence)
        {
            _observedChangeSequence = currentSequence;
            RefreshSummary();
        }

        if (_toastRemaining <= 0f)
        {
            return;
        }

        _toastRemaining -= Time.deltaTime;
        if (_toastRemaining <= 0f)
        {
            HideToast();
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void OnLootGranted(LootGrantPresentationEvent grantEvent)
    {
        if (grantEvent.Sequence == _lastGrantSequence)
        {
            return;
        }

        _lastGrantSequence = grantEvent.Sequence;
        if (!_lootReceiver.TryResolveDefinition(grantEvent.LootId, out LootDefinition definition))
        {
            ReportReadFailure();
            return;
        }

        long valueIncrement = (long)grantEvent.Amount * definition.ExtractionValuePerUnit;
        if (_toastText != null)
        {
            _toastText.text = $"+{grantEvent.Amount} {definition.DisplayName}  (+{valueIncrement})";
        }

        if (_toastRoot != null)
        {
            _toastRoot.SetActive(true);
        }

        _toastRemaining = _toastDuration;
    }

    private void RefreshSummary()
    {
        if (!_lootReceiver.TryGetLootContent(out IReadOnlyList<LootEntry> content) ||
            !_lootReceiver.TryCalculateTotalValue(out long totalValue))
        {
            ShowUnavailable();
            ReportReadFailure();
            return;
        }

        _reportedReadFailure = false;
        _contentBuilder.Clear();

        if (content.Count == 0)
        {
            _contentBuilder.Append("Loot: vacío");
        }
        else
        {
            long totalUnits = 0;
            _contentBuilder.Append("Loot transportado");

            for (int i = 0; i < content.Count; i++)
            {
                LootEntry entry = content[i];
                if (!_lootReceiver.TryResolveDefinition(entry.LootId, out LootDefinition definition))
                {
                    ShowUnavailable();
                    ReportReadFailure();
                    return;
                }

                totalUnits += entry.Amount;
                _contentBuilder.Append('\n');
                _contentBuilder.Append(definition.DisplayName);
                _contentBuilder.Append(" × ");
                _contentBuilder.Append(entry.Amount);
            }

            _contentBuilder.Append("\nTipos: ");
            _contentBuilder.Append(content.Count);
            _contentBuilder.Append("  Unidades: ");
            _contentBuilder.Append(totalUnits);
        }

        if (_contentText != null)
        {
            _contentText.text = _contentBuilder.ToString();
        }

        if (_totalValueText != null)
        {
            _totalValueText.text = $"Valor: {totalValue}";
        }
    }

    private void ShowUnavailable()
    {
        if (_contentText != null)
        {
            _contentText.text = "Loot no disponible";
        }

        if (_totalValueText != null)
        {
            _totalValueText.text = "Valor: —";
        }
    }

    private void ReportReadFailure()
    {
        if (_reportedReadFailure)
        {
            return;
        }

        _reportedReadFailure = true;
        Debug.LogError($"{nameof(LootHudPresenter)} could not resolve the complete local loot presentation.", this);
    }

    private void HideToast()
    {
        _toastRemaining = 0f;
        if (_toastRoot != null)
        {
            _toastRoot.SetActive(false);
        }
    }
}
