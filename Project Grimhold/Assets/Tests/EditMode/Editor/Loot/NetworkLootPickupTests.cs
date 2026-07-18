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
        public void LootGrantRequest_ConservesDataCorrectly()
        {
            var source = new EntityId(10);
            var receiver = new EntityId(20);
            var loot = new LootId("test_loot");
            var request = new LootGrantRequest(source, receiver, loot, 5, 100);

            Assert.AreEqual(source, request.SourceId);
            Assert.AreEqual(receiver, request.ReceiverId);
            Assert.AreEqual(loot, request.LootId);
            Assert.AreEqual(5, request.Amount);
            Assert.AreEqual(100, request.SimulationTick);
        }

        [Test]
        public void LootReceiveResult_DifferentiatesAcceptanceAndRejection()
        {
            var success = LootReceiveResult.Accepted();
            var failure = LootReceiveResult.Rejected(LootReceiveFailureReason.CapacityReached);

            Assert.IsTrue(success.Success);
            Assert.AreEqual(LootReceiveFailureReason.None, success.FailureReason);

            Assert.IsFalse(failure.Success);
            Assert.AreEqual(LootReceiveFailureReason.CapacityReached, failure.FailureReason);
        }

        [Test]
        public void PlayerLootReceiver_RejectsInvalidLootId()
        {
            var go = new GameObject();
            var receiver = go.AddComponent<PlayerLootReceiver>();
            var request = new LootGrantRequest(new EntityId(1), receiver.Id, default(LootId), 5, 100);

            var result = receiver.TryGrantLoot(request);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(LootReceiveFailureReason.InvalidLootId, result.FailureReason);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void LootId_Constructor_ValidatesInput()
        {
            Assert.Throws<ArgumentException>(() => new LootId(null));
            Assert.Throws<ArgumentException>(() => new LootId(""));
            Assert.Throws<ArgumentException>(() => new LootId("   "));
        }

        [Test]
        public void PlayerLootReceiver_RejectsInvalidAmount()
        {
            var go = new GameObject();
            var receiver = go.AddComponent<PlayerLootReceiver>();
            var request = new LootGrantRequest(new EntityId(1), receiver.Id, new LootId("gold"), 0, 100);

            var result = receiver.TryGrantLoot(request);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(LootReceiveFailureReason.InvalidAmount, result.FailureReason);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerLootReceiver_RejectionDoesNotModifyStoredState()
        {
            var go = new GameObject();
            var receiver = go.AddComponent<PlayerLootReceiver>();
            var lootId = new LootId("gold");

            // Valid grant first
            var validRequest = new LootGrantRequest(new EntityId(1), receiver.Id, lootId, 10, 100);
            receiver.TryGrantLoot(validRequest);
            Assert.AreEqual(10, receiver.GetLootAmount(lootId));

            // Invalid grant next
            var invalidRequest = new LootGrantRequest(new EntityId(1), receiver.Id, lootId, -5, 100);
            receiver.TryGrantLoot(invalidRequest);

            // Verify state is unmodified by the rejected attempt
            Assert.AreEqual(10, receiver.GetLootAmount(lootId));
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void PlayerLootReceiver_AcceptanceIncrementsQuantityCorrectly()
        {
            var go = new GameObject();
            var receiver = go.AddComponent<PlayerLootReceiver>();
            var lootId = new LootId("gold");

            var req1 = new LootGrantRequest(new EntityId(1), receiver.Id, lootId, 10, 100);
            var req2 = new LootGrantRequest(new EntityId(1), receiver.Id, lootId, 5, 101);

            receiver.TryGrantLoot(req1);
            receiver.TryGrantLoot(req2);

            Assert.AreEqual(15, receiver.GetLootAmount(lootId));
            UnityEngine.Object.DestroyImmediate(go);
        }

        private sealed class StubLootReceiver : ILootReceiver
        {
            public EntityId Id { get; }

            public StubLootReceiver(EntityId id)
            {
                Id = id;
            }

            public LootReceiveResult TryGrantLoot(in LootGrantRequest request)
            {
                return LootReceiveResult.Accepted();
            }
        }

        [Test]
        public void EntityRegistry_LocatesCorrectLootReceiver()
        {
            var registryHolder = new GameObject("EntityRegistryHolder");
            var registry = registryHolder.AddComponent<EntityRegistry>();

            var id = new EntityId(123);
            var receiver = new StubLootReceiver(id);

            bool registered = registry.TryRegisterEntity(id, receiver, new Collider2D[0]);
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

            bool r1 = registry.TryRegisterEntity(id1, receiver1, new Collider2D[0]);
            bool r2 = registry.TryRegisterEntity(id2, receiver2, new Collider2D[0]);
            Assert.IsTrue(r1);
            Assert.IsTrue(r2);

            // Unregister first one
            bool unregistered = registry.TryUnregisterEntity(id1, receiver1);
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
            bool r1 = registry.TryRegisterEntity(id, receiverOld, new Collider2D[0]);
            Assert.IsTrue(r1);

            // Unregister old
            bool u1 = registry.TryUnregisterEntity(id, receiverOld);
            Assert.IsTrue(u1);

            // Register new
            bool r2 = registry.TryRegisterEntity(id, receiverNew, new Collider2D[0]);
            Assert.IsTrue(r2);

            // Try to unregister using old reference
            bool u2 = registry.TryUnregisterEntity(id, receiverOld);
            Assert.IsFalse(u2);

            // Verify new receiver remains registered
            bool found = registry.TryGetLootReceiver(id, out var resolved);
            Assert.IsTrue(found);
            Assert.That(resolved, Is.SameAs(receiverNew));

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

        [Test]
        public void LootTransaction_PureLogic_VerifySequence()
        {
            // We verify the logical requirements of NetworkLootPickup and PlayerLootReceiver
            // under a simulated environment.
            var receiverGo = new GameObject();
            var receiver = receiverGo.AddComponent<PlayerLootReceiver>();
            var receiverId = receiver.Id;

            var loot = new LootId("coal");
            var request = new LootGrantRequest(new EntityId(99), receiverId, loot, 1, 42);

            // Test atomic grant rejection
            var rejectRequest = new LootGrantRequest(new EntityId(99), receiverId, loot, -10, 42);
            var rejectResult = receiver.TryGrantLoot(rejectRequest);
            Assert.IsFalse(rejectResult.Success);
            Assert.AreEqual(0, receiver.GetLootAmount(loot));

            // Test atomic grant acceptance
            var acceptResult = receiver.TryGrantLoot(request);
            Assert.IsTrue(acceptResult.Success);
            Assert.AreEqual(1, receiver.GetLootAmount(loot));

            UnityEngine.Object.DestroyImmediate(receiverGo);
        }
    }
}
