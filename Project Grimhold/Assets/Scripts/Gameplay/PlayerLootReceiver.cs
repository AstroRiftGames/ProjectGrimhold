using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authoritative container component that stores received loot per LootId on the player.
/// Integrates with EntityRegistry using the player's EntityId.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerLootReceiver : MonoBehaviour, ILootReceiver
{
    private readonly Dictionary<LootId, int> _lootInventory = new();
    private EntityId _registeredId;
    private EntityRegistry _registry;
    private Collider2D[] _cachedColliders;
    private bool _isRegistered;

    public EntityId Id
    {
        get
        {
            var character = GetComponent<ICharacter>();
            if (character != null)
            {
                return character.Id;
            }
            return new EntityId(gameObject.GetHashCode());
        }
    }

    private void Awake()
    {
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void Start()
    {
        var runner = GetComponentInParent<Fusion.NetworkRunner>();
        if (runner != null)
        {
            _registry = runner.GetComponent<EntityRegistry>();
            if (_registry != null)
            {
                _registeredId = Id;
                _isRegistered = _registry.TryRegisterEntity(_registeredId, this, _cachedColliders);
            }
        }
    }

    private void OnDestroy()
    {
        if (_isRegistered && _registry != null)
        {
            _registry.TryUnregisterEntity(_registeredId, this);
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
