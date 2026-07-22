using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Loot
{
    public sealed class EntityRegistryLootSourceTests
    {
        private GameObject _registryObject;
        private EntityRegistry _registry;
        private GameObject _firstObject;
        private GameObject _secondObject;

        [SetUp]
        public void SetUp()
        {
            _registryObject = new GameObject("Registry");
            _registry = _registryObject.AddComponent<EntityRegistry>();
            _firstObject = new GameObject("First source");
            _secondObject = new GameObject("Second source");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_secondObject);
            Object.DestroyImmediate(_firstObject);
            Object.DestroyImmediate(_registryObject);
        }

        [Test]
        public void GroupedRegistration_RegistersBothCapabilitiesAndColliderAtomically()
        {
            var source = _firstObject.AddComponent<Source>();
            source.IdValue = 10;
            Collider2D collider = _firstObject.AddComponent<BoxCollider2D>();

            Assert.That(_registry.TryRegisterLootSource(source.Id, source, source, new[] { collider }), Is.True);
            Assert.That(_registry.TryGetLootSource(source.Id, out ILootExtractor extractor, out ILootQuantityReader reader), Is.True);
            Assert.That(extractor, Is.SameAs(source));
            Assert.That(reader, Is.SameAs(source));
            Assert.That(_registry.TryGetEntityId(collider, out EntityId colliderId), Is.True);
            Assert.That(colliderId, Is.EqualTo(source.Id));
        }

        [Test]
        public void ColliderConflict_LeavesNoPartialLootSourceRegistration()
        {
            var owner = _firstObject.AddComponent<Source>();
            owner.IdValue = 10;
            Collider2D collider = _firstObject.AddComponent<BoxCollider2D>();
            Assert.That(_registry.TryRegisterLootSource(owner.Id, owner, owner, new[] { collider }), Is.True);

            var conflicting = _secondObject.AddComponent<Source>();
            conflicting.IdValue = 20;
            Assert.That(_registry.TryRegisterLootSource(conflicting.Id, conflicting, conflicting, new[] { collider }), Is.False);
            Assert.That(_registry.TryGetLootSource(conflicting.Id, out _, out _), Is.False);
            Assert.That(_registry.TryGetEntityId(collider, out EntityId mapped), Is.True);
            Assert.That(mapped, Is.EqualTo(owner.Id));
        }

        [Test]
        public void Unregister_RequiresExpectedInstancesAndRemovesOnlyOwnedCollider()
        {
            var source = _firstObject.AddComponent<Source>();
            source.IdValue = 10;
            Collider2D collider = _firstObject.AddComponent<BoxCollider2D>();
            _registry.TryRegisterLootSource(source.Id, source, source, new[] { collider });
            var wrong = _secondObject.AddComponent<Source>();
            wrong.IdValue = 10;

            Assert.That(_registry.TryUnregisterLootSource(source.Id, wrong, wrong), Is.False);
            Assert.That(_registry.TryGetLootSource(source.Id, out _, out _), Is.True);
            Assert.That(_registry.TryUnregisterLootSource(source.Id, source, source), Is.True);
            Assert.That(_registry.TryGetLootSource(source.Id, out _, out _), Is.False);
            Assert.That(_registry.TryGetEntityId(collider, out _), Is.False);
        }

        private sealed class Source : MonoBehaviour, ILootExtractor, ILootQuantityReader
        {
            public int IdValue { get; set; }
            public EntityId Id => new(IdValue);
            public int GetLootAmount(LootId lootId) => 1;
            public LootTransferFailureReason ValidateExtraction(in LootTransferRequest request) => LootTransferFailureReason.None;
            public void CommitExtraction(in LootTransferRequest request) { }
        }
    }
}
