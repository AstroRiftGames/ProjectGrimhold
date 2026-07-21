using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds the local player's replicated raid inventory to a read-only slot view.
/// It observes snapshots and change sequence but never validates or commits loot transfers.
/// </summary>
[DisallowMultipleComponent]
public sealed class RaidInventoryPresenter : MonoBehaviour
{
    [SerializeField]
    private RaidInventoryView _view;

    private readonly List<LootEntry> _projectedEntries = new();
    private readonly List<RaidInventorySlotData> _slotData = new();
    private readonly HashSet<LootId> _reportedMissingDefinitions = new();

    private PlayerLootReceiver _lootReceiver;
    private PlayerInputReader _inputReader;
    private IDisposable _inputSuppression;
    private int _observedChangeSequence;
    private bool _isBound;
    private bool _isSubscribed;
    private bool _reportedSnapshotFailure;

    public void Bind(PlayerLootReceiver lootReceiver, PlayerInputReader inputReader)
    {
        Unbind();

        if (lootReceiver == null || inputReader == null || _view == null)
        {
            return;
        }

        _lootReceiver = lootReceiver;
        _inputReader = inputReader;
        _isBound = true;

        if (isActiveAndEnabled)
        {
            SubscribeToInput();
            RefreshFromCurrentSnapshot();
        }
    }

    public void Unbind()
    {
        Close();
        UnsubscribeFromInput();

        _lootReceiver = null;
        _inputReader = null;
        _observedChangeSequence = 0;
        _isBound = false;
        _reportedSnapshotFailure = false;
        _reportedMissingDefinitions.Clear();
        _projectedEntries.Clear();
        _slotData.Clear();

        if (_view != null)
        {
            _view.ClearContent();
        }
    }

    public void Close()
    {
        if (_view != null)
        {
            _view.SetScreenVisible(false);
        }

        ReleaseInputSuppression();
    }

    private void OnEnable()
    {
        if (!_isBound)
        {
            return;
        }

        SubscribeToInput();
        RefreshFromCurrentSnapshot();
        Close();
    }

    private void OnDisable()
    {
        Close();
        UnsubscribeFromInput();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Update()
    {
        if (!_isBound || _lootReceiver == null)
        {
            return;
        }

        int currentSequence = _lootReceiver.LootChangeSequence;
        if (currentSequence == _observedChangeSequence)
        {
            return;
        }

        RefreshFromCurrentSnapshot();
    }

    private void OnInventoryToggleRequested()
    {
        if (_view != null && _view.IsOpen)
        {
            Close();
            return;
        }

        Open();
    }

    private void Open()
    {
        if (!_isBound || _inputReader == null || _view == null || _view.IsOpen)
        {
            return;
        }

        RefreshFromCurrentSnapshot();
        _inputSuppression = _inputReader.AcquireGameplayInputSuppression();
        _view.SetScreenVisible(true);
    }

    private void RefreshFromCurrentSnapshot()
    {
        if (!_isBound || _lootReceiver == null || _view == null)
        {
            return;
        }

        _observedChangeSequence = _lootReceiver.LootChangeSequence;
        int capacity = _lootReceiver.SlotCapacity;

        if (!_view.EnsureSlotCount(capacity) ||
            !_lootReceiver.TryGetLootContent(out IReadOnlyList<LootEntry> content) ||
            !RaidInventoryProjection.TryBuild(content, capacity, _projectedEntries))
        {
            _view.ShowUnavailable();
            ReportSnapshotFailure();
            return;
        }

        _reportedSnapshotFailure = false;
        _slotData.Clear();

        for (int i = 0; i < _projectedEntries.Count; i++)
        {
            LootEntry entry = _projectedEntries[i];
            if (!entry.IsValid)
            {
                _slotData.Add(RaidInventorySlotData.Empty);
                continue;
            }

            if (_lootReceiver.TryResolveDefinition(entry.LootId, out LootDefinition definition))
            {
                _slotData.Add(RaidInventorySlotData.Create(
                    entry,
                    definition,
                    _view.PlaceholderIcon));
                continue;
            }

            _slotData.Add(RaidInventorySlotData.Create(
                entry,
                null,
                _view.PlaceholderIcon));
            ReportMissingDefinition(entry.LootId);
        }

        long? totalValue = _lootReceiver.TryCalculateTotalValue(out long calculatedValue)
            ? calculatedValue
            : null;

        if (!_view.Present(_slotData, totalValue))
        {
            _view.ShowUnavailable();
            ReportSnapshotFailure();
        }
    }

    private void SubscribeToInput()
    {
        if (_isSubscribed || _inputReader == null)
        {
            return;
        }

        _inputReader.InventoryToggleRequested += OnInventoryToggleRequested;
        _isSubscribed = true;
    }

    private void UnsubscribeFromInput()
    {
        if (!_isSubscribed || _inputReader == null)
        {
            _isSubscribed = false;
            return;
        }

        _inputReader.InventoryToggleRequested -= OnInventoryToggleRequested;
        _isSubscribed = false;
    }

    private void ReleaseInputSuppression()
    {
        _inputSuppression?.Dispose();
        _inputSuppression = null;
    }

    private void ReportSnapshotFailure()
    {
        if (_reportedSnapshotFailure)
        {
            return;
        }

        _reportedSnapshotFailure = true;
        Debug.LogError($"{nameof(RaidInventoryPresenter)} could not build the complete local inventory snapshot.", this);
    }

    private void ReportMissingDefinition(LootId lootId)
    {
        if (!_reportedMissingDefinitions.Add(lootId))
        {
            return;
        }

        Debug.LogError($"{nameof(RaidInventoryPresenter)} could not resolve metadata for loot '{lootId.Value}'.", this);
    }
}
