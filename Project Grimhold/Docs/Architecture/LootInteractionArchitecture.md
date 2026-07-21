# Loot Interaction and Transfer Architecture

## 1. Identity, configuration, and stacks

`LootId` is the stable, comparable domain identifier and is independent of Unity and Fusion. `LootDefinition` contains shared configuration only, while `LootDefinitionCatalog` is the local source of truth for resolving that configuration.

The catalog assigns deterministic indices by sorting IDs with ordinal comparison and supports `LootId ↔ index` resolution. Only indices and quantities are replicated; names, icons, rarity, and value are resolved locally.

`LootEntry` is the only value object representing an aggregated stack. It contains a `LootId`, quantity, derived validity, and value equality. There is no parallel `LootStack` type.

Until maximum stack sizes are introduced, gameplay slot occupancy is defined as:

```text
occupied slots = number of distinct LootIds with a positive quantity
```

- Increasing an existing ID does not consume another slot.
- Adding a new ID requires a free slot.
- There is no automatic splitting, weight capacity, or per-stack limit.
- Numeric overflow is rejected.

The capacity of `64` on `PlayerLootReceiver.NetworkDictionary` is exclusively a Fusion representation limit. It does not represent gameplay slots or inventory capacity.

## 2. Unified transfer model

All loot movement uses one vocabulary:

- `LootTransferRequest`: source, destination, `LootId`, requested quantity, and simulation tick.
- `LootTransferResult`: complete success or rejection, transferred quantity, and typed reason.
- `LootTransferFailureReason`: stable domain reasons without Fusion, UI, or presentation details.

Requests and results are immutable. Success always represents the complete requested quantity and uses `None`. Rejection transfers zero and uses a reason other than `None`. Partial success cannot be represented.

A definition missing from the catalog maps to `InvalidLoot`. Fusion-specific index or technical-capacity failures are diagnosed at the integration boundary and exposed as `ContainerUnavailable`, never as full gameplay capacity.

## 3. Segregated capabilities

Entities implement only the capabilities they require:

- `ILootContentReader`: produces a complete read-only snapshot.
- `ILootQuantityReader`: queries the aggregated quantity for a `LootId`.
- `ILootSlotCapacityReader`: exposes gameplay capacity and occupancy.
- `ILootReceiver`: prevalidates and commits reception.
- `ILootExtractor`: prevalidates and commits extraction.

`PlayerLootReceiver` implements content reading, quantity queries, gameplay slot capacity, reception, and extraction. A pickup is not an inspectable container and does not implement reading, slots, or extraction.

## 4. Prevalidation and commit protocol

Reception and extraction explicitly separate two phases:

1. `ValidateReceive` or `ValidateExtraction` checks every endpoint precondition without mutating state and returns a typed reason.
2. `CommitReceive` or `CommitExtraction` applies exactly the requested quantity without repeating gameplay validation or returning a rejection.

A commit may run only after prevalidation returns `None`, synchronously, and without State Authority yielding control or allowing an intervening mutation. If its internal preconditions no longer hold, the integration contract has been violated; this is not a normal gameplay rejection.

TASK-31 still does not implement an atomic runtime transfer between two storage endpoints. A later task must resolve both endpoints, validate authority, distance, and availability, exclude competing requests, prevalidate both sides, define commit order, and execute both commits without re-entry before producing the single `LootTransferResult`.

## 5. Authoritative pickup transaction

`NetworkLootPickup` is a consumable source with its own reservation, not an extractable storage endpoint or a general coordinator. Under State Authority it:

1. Validates interaction and availability.
2. Resolves `ILootReceiver` through `EntityRegistry`.
3. Builds a `LootTransferRequest`.
4. Reserves the pickup with `IsConsumed = true`.
5. Calls `ValidateReceive`.
6. Restores the reservation and maps the result to `InteractionResult` when rejected.
7. Calls `CommitReceive` immediately when accepted.
8. Despawns the pickup only after the complete commit.

The reservation prevents two authoritative requests from delivering the same reward. The pickup does not know the player's internal implementation.

Interaction retains a general-purpose result: missing authority, destination, and range map to their interaction equivalents, while other loot rejections map to `LootRejected`. `InteractionResult` and `LootTransferResult` remain separate.

## 6. Temporary storage and authority

`PlayerLootReceiver` stores the incursion collection in a `NetworkDictionary<int,int>` attached to the player's `NetworkObject`. State Authority is the only writer. Other peers consume replicated snapshots.

Gameplay capacity is a positive serialized value on the component, configured to 16 slots on the base network-player prefab and limited to the dictionary's technical maximum. It is not networked and is not derived from the dictionary capacity. `SlotCapacity` exposes that configuration, while `OccupiedSlotCount` is derived from the number of stored keys. Commits preserve the invariant that every stored quantity is positive.

`ValidateReceive` checks authority, IDs, loot, quantity, catalog resolution, representation, overflow, and gameplay capacity without mutating the collection. Existing IDs stack without consuming another slot. A new ID is rejected with `InventoryFull` only when all configured gameplay slots are occupied. `CommitReceive` applies the complete prevalidated quantity, increments `LootChangeSequence`, and preserves the existing presentation integration.

`ValidateExtraction` checks authority, endpoint identities, loot, quantity, catalog resolution, representation, and complete availability without mutation. `CommitExtraction` subtracts the complete prevalidated quantity, removes the key when its amount reaches zero, and increments `LootChangeSequence`. Extraction does not resolve or modify the destination and does not emit a grant presentation event.

The collection registers as `ILootReceiver` in the State Authority runner's `EntityRegistry` and unregisters when the player despawns. Its state is created and destroyed with that object; there is no persistence to a stash, equipment, or another session.

## 7. Presentation

`LootChangeSequence`, the RPC directed to Input Authority, and `LootGrantPresentationEvent` remain integration and presentation responsibilities. Transfer contracts do not contain sequences, RPCs, text, icons, presenters, or visual references.

The local raid-inventory HUD reads snapshots through `TryGetLootContent`, capacity through `SlotCapacity`, derives value and visual metadata locally, and observes both reception and extraction through `LootChangeSequence`. It preserves the snapshot order and maintains a fixed visual slot pool without owning another inventory collection.

`LootGrantPresentationEvent` remains specific to the current pickup delivery and continues to drive the transient pickup toast. Generalizing it belongs to a later task when container-transfer presentation exists.

See `Docs/Architecture/RaidInventoryUIArchitecture.md` for the local binding, input suppression, lifecycle, slot projection, and future container-panel composition defined by TASK-32.

## 8. Future work

The following remain outside TASK-31:

- Authoritative coordination between an extractable source and a receiving destination.
- Resolution of new capabilities through `EntityRegistry`.
- Containers, chests, corpses, and inspection.
- Container distance and availability validation.
- Exclusion of concurrent transfers affecting the same stack.
- Generalized presentation and Host/Client tests between storage endpoints.
