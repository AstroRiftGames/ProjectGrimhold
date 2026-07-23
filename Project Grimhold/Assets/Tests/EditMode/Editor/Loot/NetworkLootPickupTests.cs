using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Loot
{
    [TestFixture]
    public class NetworkLootPickupTests
    {
        private LootDefinition _lootDefinition;
        private Sprite _testSprite;
        private Texture2D _testTexture;

        [SetUp]
        public void SetUp()
        {
            _lootDefinition = ScriptableObject.CreateInstance<LootDefinition>();
            _testTexture = new Texture2D(8, 8);
            _testSprite = Sprite.Create(_testTexture, new Rect(0, 0, 8, 8), Vector2.zero);
            
            // Initialize LootDefinition private fields using reflection to pass validation
            SetField(_lootDefinition, "_id", "test_item");
            SetField(_lootDefinition, "_displayName", "Test Item");
            SetField(_lootDefinition, "_icon", _testSprite);
            SetField(_lootDefinition, "_worldSprite", _testSprite);
            SetField(_lootDefinition, "_category", LootCategory.Valuable);
            SetField(_lootDefinition, "_rarity", LootRarity.Common);
            SetField(_lootDefinition, "_extractionValuePerUnit", 10);
            SetField(_lootDefinition, "_defaultPickupQuantity", 1);
        }

        [TearDown]
        public void TearDown()
        {
            if (_lootDefinition != null) UnityEngine.Object.DestroyImmediate(_lootDefinition);
            if (_testSprite != null) UnityEngine.Object.DestroyImmediate(_testSprite);
            if (_testTexture != null) UnityEngine.Object.DestroyImmediate(_testTexture);
        }

        private void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(target, value);
        }

        [Test]
        public void PlayerLootReceiver_WithoutSpawnedStateAuthorityRejectsPrevalidation()
        {
            var go = new GameObject();
            var receiver = go.AddComponent<PlayerLootReceiver>();
            var request = new LootTransferRequest(
                new EntityId(1),
                new EntityId(2),
                new LootId("test_loot"),
                5,
                100);

            LootTransferFailureReason failureReason = receiver.ValidateReceive(request);

            Assert.AreEqual(LootTransferFailureReason.MissingAuthority, failureReason);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void LootId_Constructor_ValidatesInput()
        {
            Assert.Throws<ArgumentException>(() => new LootId(null));
            Assert.Throws<ArgumentException>(() => new LootId(""));
            Assert.Throws<ArgumentException>(() => new LootId("   "));
        }

        private sealed class StubLootReceiver : ILootReceiver
        {
            public EntityId ID { get; }

            public int CommittedAmount { get; private set; }

            public StubLootReceiver(EntityId id)
            {
                ID = id;
            }

            public LootTransferFailureReason ValidateReceive(in LootTransferRequest request)
            {
                return request.DestinationId == ID
                    ? LootTransferFailureReason.None
                    : LootTransferFailureReason.DestinationNotFound;
            }

            public void CommitReceive(in LootTransferRequest request)
            {
                CommittedAmount += request.RequestedAmount;
            }
        }

        [Test]
        public void EntityRegistry_LocatesCorrectLootReceiver()
        {
            var registryHolder = new GameObject("EntityRegistryHolder");
            var registry = registryHolder.AddComponent<EntityRegistry>();

            var id = new EntityId(123);
            var receiver = new StubLootReceiver(id);

            bool registered = registry.TryRegisterLootReceiver(id, receiver);
            Assert.IsTrue(registered);

            bool found = registry.TryGetLootReceiver(id, out var foundReceiver);
            Assert.IsTrue(found);
            Assert.That(foundReceiver, Is.SameAs(receiver));

            UnityEngine.Object.DestroyImmediate(registryHolder);
        }

        [Test]
        public void EntityRegistry_UnregistersOnlyExpectedInstance()
        {
            var registryHolder = new GameObject("EntityRegistryHolder");
            var registry = registryHolder.AddComponent<EntityRegistry>();

            var id1 = new EntityId(123);
            var id2 = new EntityId(456);
            var receiver1 = new StubLootReceiver(id1);
            var receiver2 = new StubLootReceiver(id2);

            bool r1 = registry.TryRegisterLootReceiver(id1, receiver1);
            bool r2 = registry.TryRegisterLootReceiver(id2, receiver2);
            Assert.IsTrue(r1);
            Assert.IsTrue(r2);

            // Unregister first one
            bool unregistered = registry.TryUnregisterLootReceiver(id1, receiver1);
            Assert.IsTrue(unregistered);

            Assert.IsFalse(registry.TryGetLootReceiver(id1, out _));
            Assert.IsTrue(registry.TryGetLootReceiver(id2, out var found2));
            Assert.That(found2, Is.SameAs(receiver2));

            UnityEngine.Object.DestroyImmediate(registryHolder);
        }

        [Test]
        public void EntityRegistry_ObsoleteInstanceCannotUnregisterPosterior()
        {
            var registryHolder = new GameObject("EntityRegistryHolder");
            var registry = registryHolder.AddComponent<EntityRegistry>();

            var id = new EntityId(123);
            var receiverOld = new StubLootReceiver(id);
            var receiverNew = new StubLootReceiver(id);

            // Register first
            bool r1 = registry.TryRegisterLootReceiver(id, receiverOld);
            Assert.IsTrue(r1);

            // Unregister old
            bool u1 = registry.TryUnregisterLootReceiver(id, receiverOld);
            Assert.IsTrue(u1);

            // Register new
            bool r2 = registry.TryRegisterLootReceiver(id, receiverNew);
            Assert.IsTrue(r2);

            // Try to unregister using old reference
            bool u2 = registry.TryUnregisterLootReceiver(id, receiverOld);
            Assert.IsFalse(u2);

            // Verify new receiver remains registered
            bool found = registry.TryGetLootReceiver(id, out var resolved);
            Assert.IsTrue(found);
            Assert.That(resolved, Is.SameAs(receiverNew));

            UnityEngine.Object.DestroyImmediate(registryHolder);
        }

        private sealed class DummyDamageable : IDamageable
        {
            public EntityId ID { get; }
            public bool CanReceiveDamage => true;

            public DummyDamageable(EntityId id)
            {
                ID = id;
            }

            public DamageResult ApplyDamage(in DamageRequest request)
            {
                return new DamageResult(ID, true, request.Amount, 0f, true, DamageFailureReason.None);
            }
        }

        [Test]
        public void EntityRegistry_CapacitiesCoexistAndUnregisterIndependently()
        {
            var registryHolder = new GameObject("EntityRegistryHolder");
            var registry = registryHolder.AddComponent<EntityRegistry>();

            var id = new EntityId(999);
            var receiver = new StubLootReceiver(id);
            var damageable = new DummyDamageable(id);

            // Register damageable via standard path
            bool rD = registry.TryRegisterEntity(id, damageable, new Collider2D[0]);
            Assert.IsTrue(rD);

            // Register loot receiver via custom path
            bool rL = registry.TryRegisterLootReceiver(id, receiver);
            Assert.IsTrue(rL);

            // Both should coexist under same EntityId
            Assert.IsTrue(registry.TryGetDamageable(id, out var resolvedDamageable));
            Assert.That(resolvedDamageable, Is.SameAs(damageable));

            Assert.IsTrue(registry.TryGetLootReceiver(id, out var resolvedLoot));
            Assert.That(resolvedLoot, Is.SameAs(receiver));

            // Unregistering LootReceiver should not remove Damageable
            bool uL = registry.TryUnregisterLootReceiver(id, receiver);
            Assert.IsTrue(uL);
            Assert.IsFalse(registry.TryGetLootReceiver(id, out _));
            Assert.IsTrue(registry.TryGetDamageable(id, out _));

            // Clean up
            UnityEngine.Object.DestroyImmediate(registryHolder);
        }

        [Test]
        public void InteractionResult_SucceededDoesNotConsumeByDefault()
        {
            var res = InteractionResult.Succeeded();
            Assert.IsTrue(res.Success);
            Assert.IsFalse(res.IsConsumed);
            Assert.AreEqual(InteractionFailureReason.None, res.FailureReason);
        }

        [Test]
        public void InteractionResult_RejectedContainsTypedFailureReason()
        {
            var res = InteractionResult.Rejected(InteractionFailureReason.ReceiverNotFound);
            Assert.IsFalse(res.Success);
            Assert.AreEqual(InteractionFailureReason.ReceiverNotFound, res.FailureReason);
        }

    }
}
