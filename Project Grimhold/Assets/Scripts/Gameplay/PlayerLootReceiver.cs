using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Authoritative container component that stores received loot per LootId on the player.
/// Integrates with EntityRegistry using the player's EntityId.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerLootReceiver : NetworkBehaviour, ILootReceiver
{
    private readonly Dictionary<LootId, int> _lootInventory = new();
    private EntityRegistry _registry;
    private ICharacter _character;
    private bool _isRegistered;
    private EntityId _registeredId;

    public new EntityId Id
    {
        get
        {
            if (_character != null)
            {
                return _character.Id;
            }
            return default;
        }
    }

    private void Awake()
    {
        _character = GetComponent<ICharacter>();
    }

    public override void Spawned()
    {
        if (_character == null)
        {
            Debug.LogError($"{nameof(PlayerLootReceiver)}: No component implementing {nameof(ICharacter)} is found on {gameObject.name}.", this);
            return;
        }

        _registry = Runner.GetComponent<EntityRegistry>();
        if (_registry != null)
        {
            _registeredId = Id;
            _isRegistered = _registry.TryRegisterLootReceiver(_registeredId, this);
            if (!_isRegistered)
            {
                Debug.LogError($"{nameof(PlayerLootReceiver)}: Failed to register to {nameof(EntityRegistry)} with ID {_registeredId}.", this);
            }
        }
        else
        {
            Debug.LogError($"{nameof(PlayerLootReceiver)}: EntityRegistry was not found on the NetworkRunner.", this);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_isRegistered && _registry != null)
        {
            _registry.TryUnregisterLootReceiver(_registeredId, this);
            _isRegistered = false;
        }
    }

    public LootReceiveResult TryGrantLoot(in LootGrantRequest request)
    {
        if (request.ReceiverId != Id)
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.ReceiverUnavailable);
        }

        if (string.IsNullOrWhiteSpace(request.LootId.Value))
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.InvalidLootId);
        }

        if (request.Amount <= 0)
        {
            return LootReceiveResult.Rejected(LootReceiveFailureReason.InvalidAmount);
        }

        // Perform the grant atomically
        if (!_lootInventory.TryGetValue(request.LootId, out int currentAmount))
        {
            currentAmount = 0;
        }

        _lootInventory[request.LootId] = currentAmount + request.Amount;
        return LootReceiveResult.Accepted();
    }

    /// <summary>
    /// Gets the amount of a specific loot item currently held (for tests and verification).
    /// </summary>
    public int GetLootAmount(LootId lootId)
    {
        return _lootInventory.TryGetValue(lootId, out int amount) ? amount : 0;
    }
}
