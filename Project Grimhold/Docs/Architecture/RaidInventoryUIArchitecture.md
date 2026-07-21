# Raid Inventory UI Architecture

## Context and decision

TASK-32 replaces the provisional textual loot summary with a local, read-only uGUI slot screen. `PlayerLootReceiver` remains the only inventory source of truth and State Authority remains the only writer.

The local presentation flow is:

```text
PlayerLootReceiver
    -> snapshot, capacity, LootChangeSequence and local metadata
    -> RaidInventoryPresenter
    -> RaidInventoryProjection
    -> RaidInventoryView
    -> RaidInventorySlotView
```

The presenter calls only `TryGetLootContent`, `SlotCapacity`, `LootChangeSequence`, `TryResolveDefinition`, and `TryCalculateTotalValue`. It never validates or commits reception or extraction and never accesses the network dictionary directly.

## Slot projection and metadata

`PlayerLootReceiver.TryGetLootContent` already emits catalog-index order. `RaidInventoryProjection` preserves that order, rejects content beyond gameplay capacity, and appends empty entries until the projection length equals `SlotCapacity`.

The view creates a stable slot pool when binding or capacity changes. Normal content refreshes reuse those views. A missing icon uses the serialized project placeholder. If a complete definition cannot be resolved, only that slot degrades to the placeholder, raw `LootId` text, and replicated quantity; the presenter reports the integration error once per ID and keeps other slots visible.

The screen contains a `PanelsRow` with the player panel as its only child. A future container panel may be composed as a sibling, but TASK-32 contains no container runtime, selection, transfer controls, or drag and drop.

## Local input boundary

`PlayerInputReader` owns one `PlayerInputActions` instance with the normal `Gameplay` map and a local-only `LocalUI.ToggleInventory` action bound provisionally to Tab. The toggle is not part of `PlayerNetworkInput` or `PlayerInputButton`.

Opening the screen acquires a small owner-specific suppression token. While any token exists, `ConsumeNetworkInput` returns `default(PlayerNetworkInput)` and discrete attack/interaction buffers are discarded. Gameplay and LocalUI action maps remain under their normal component lifecycle and are not toggled by suppression.

On final release, movement and aim are read directly from the current action and pointer state so held continuous controls resume immediately. Attack and interaction that remain held must first be released; only a subsequent valid press can be transported. This prevents a button pressed behind the inventory from becoming a delayed gameplay edge.

## Runner-scoped binding and lifecycle

`LocalInputContext` is a local-only component created on the runner. It stores at most one active `PlayerInputReader`, notifies changes, and clears on shutdown. It contains no networked state, inventory knowledge, or general service registry. `FusionInputProvider` registers its serialized reader through the runner reference obtained by its existing lookup flow. Replacing that lookup is separate technical debt.

`LocalPlayerHudBinder` binds only the Input Authority player's receiver and the reader exposed by its runner context. The inventory presenter remains outside the visual screen root.

- `Close` hides the screen and releases suppression while retaining binding and slots.
- `OnDisable` closes and unsubscribes but retains receiver and reader references.
- `OnEnable` resubscribes and rebuilds from the current snapshot without a new binder call.
- `Unbind` releases suppression, removes subscriptions, clears references, sequence state, diagnostics, and visual content.
- `OnDestroy` performs the same cleanup idempotently.

Player despawn, runner shutdown, scene unload, or reader replacement therefore cannot leave local gameplay input suppressed. A later session creates a new runner context and receives a fresh player inventory.

## Validation strategy

Pure tests cover projection order/capacity and slot fallback data. Input tests cover continuous restoration, discrete rearming, nested suppression, and the local toggle. Play Mode view tests cover stable slot reuse and clearing. Final binding, replication, despawn, and Host/Client isolation require manual multiplayer validation with a spawned `PlayerLootReceiver`; TASK-32 adds no new Fusion test infrastructure.
