using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Loot
{
    public sealed class EntityRegistryLootCompositionTests
    {
        private GameObject _registryObject;
        private GameObject _sourceObject;
        private GameObject _interactableObject;
        private EntityRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registryObject = new GameObject("Registry");
            _sourceObject = new GameObject("Source");
            _interactableObject = new GameObject("Interactable");
            _registry = _registryObject.AddComponent<EntityRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_interactableObject);
            Object.DestroyImmediate(_sourceObject);
            Object.DestroyImmediate(_registryObject);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SourceAndInteractable_CanRegisterInEitherOrder(bool sourceFirst)
        {
            var id = new EntityId(41);
            Source source = _sourceObject.AddComponent<Source>();
            source.IdValue = id.Value;
            Interactable interactable = _interactableObject.AddComponent<Interactable>();
            interactable.IdValue = id.Value;
            Collider2D collider = _sourceObject.AddComponent<BoxCollider2D>();

            if (sourceFirst)
            {
                Assert.That(_registry.TryRegisterLootSource(id, source, source, new[] { collider }), Is.True);
                Assert.That(_registry.TryRegisterInteractable(id, interactable), Is.True);
            }
            else
            {
                Assert.That(_registry.TryRegisterInteractable(id, interactable), Is.True);
                Assert.That(_registry.TryRegisterLootSource(id, source, source, new[] { collider }), Is.True);
            }

            Assert.That(_registry.TryGetInteractable(id, out IInteractable resolved), Is.True);
            Assert.That(resolved, Is.SameAs(interactable));
            Assert.That(_registry.TryGetLootSource(id, out ILootExtractor extractor, out _), Is.True);
            Assert.That(extractor, Is.SameAs(source));
            Assert.That(_registry.TryGetEntityId(collider, out EntityId colliderId), Is.True);
            Assert.That(colliderId, Is.EqualTo(id));
        }

        [Test]
        public void IndependentUnregister_PreservesTheOtherOwner()
        {
            var id = new EntityId(42);
            Source source = _sourceObject.AddComponent<Source>();
            source.IdValue = id.Value;
            Interactable interactable = _interactableObject.AddComponent<Interactable>();
            interactable.IdValue = id.Value;
            Collider2D collider = _sourceObject.AddComponent<BoxCollider2D>();
            _registry.TryRegisterLootSource(id, source, source, new[] { collider });
            _registry.TryRegisterInteractable(id, interactable);

            Assert.That(_registry.TryUnregisterInteractable(id, interactable), Is.True);
            Assert.That(_registry.TryGetLootSource(id, out _, out _), Is.True);
            Assert.That(_registry.TryGetEntityId(collider, out _), Is.True);

            Assert.That(_registry.TryRegisterInteractable(id, interactable), Is.True);
            Assert.That(_registry.TryUnregisterLootSource(id, source, source), Is.True);
            Assert.That(_registry.TryGetInteractable(id, out IInteractable resolved), Is.True);
            Assert.That(resolved, Is.SameAs(interactable));
            Assert.That(_registry.TryGetEntityId(collider, out _), Is.False);
        }

        [Test]
        public void ConflictingAndObsoleteInteractables_CannotReplaceOrRemoveCurrentOwner()
        {
            var id = new EntityId(43);
            Interactable first = _interactableObject.AddComponent<Interactable>();
            first.IdValue = id.Value;
            Interactable second = _sourceObject.AddComponent<Interactable>();
            second.IdValue = id.Value;

            Assert.That(_registry.TryRegisterInteractable(id, first), Is.True);
            Assert.That(_registry.TryRegisterInteractable(id, first), Is.True);
            Assert.That(_registry.TryRegisterInteractable(id, second), Is.False);
            Assert.That(_registry.TryUnregisterInteractable(id, second), Is.False);
            Assert.That(_registry.TryGetInteractable(id, out IInteractable resolved), Is.True);
            Assert.That(resolved, Is.SameAs(first));

            Assert.That(_registry.TryUnregisterInteractable(id, first), Is.True);
            Assert.That(_registry.TryRegisterInteractable(id, second), Is.True);
            Assert.That(_registry.TryUnregisterInteractable(id, first), Is.False);
            Assert.That(_registry.TryGetInteractable(id, out resolved), Is.True);
            Assert.That(resolved, Is.SameAs(second));
        }

        private sealed class Source : MonoBehaviour, ILootExtractor, ILootQuantityReader
        {
            public int IdValue { get; set; }
            public EntityId Id => new(IdValue);
            public int GetLootAmount(LootId lootId) => 1;
            public LootTransferFailureReason ValidateExtraction(in LootTransferRequest request) => LootTransferFailureReason.None;
            public void CommitExtraction(in LootTransferRequest request) { }
        }

        private sealed class Interactable : MonoBehaviour, IInteractable
        {
            public int IdValue { get; set; }
            public EntityId Id => new(IdValue);
            public bool CanInteract(in InteractionRequest request) => true;
            public InteractionResult Interact(in InteractionRequest request) => InteractionResult.Succeeded();
        }
    }
}
