using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Player
{
    public class EntityRegistryInteractionTests
    {
        private GameObject _registryHolder;
        private EntityRegistry _registry;
        private List<GameObject> _spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            _spawnedObjects = new List<GameObject>();
            _registryHolder = new GameObject("EntityRegistryHolder");
            _registry = _registryHolder.AddComponent<EntityRegistry>();
            _spawnedObjects.Add(_registryHolder);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawnedObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private (GameObject go, DummyInteractable interactable) CreateInteractable(EntityId id, Vector2 position)
        {
            var go = new GameObject($"Interactable_{id.Value}");
            go.transform.position = position;
            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;

            var interactable = go.AddComponent<DummyInteractable>();
            interactable.IdValue = id.Value;

            _spawnedObjects.Add(go);
            return (go, interactable);
        }

        [Test]
        public void TryRegisterEntity_RegistersValidInteractableAndResolvesCollider()
        {
            var id = new EntityId(10);
            var (go, interactable) = CreateInteractable(id, Vector2.zero);
            var colliders = new[] { go.GetComponent<Collider2D>() };

            bool registered = _registry.TryRegisterEntity(id, interactable, colliders);

            Assert.IsTrue(registered);
            Assert.IsTrue(_registry.TryGetInteractable(id, out var resolved));
            Assert.AreSame(interactable, resolved);

            Assert.IsTrue(_registry.TryGetEntityId(colliders[0], out var resolvedId));
            Assert.AreEqual(id, resolvedId);
        }

        [Test]
        public void TryRegisterEntity_IdempotentOnSameInstance()
        {
            var id = new EntityId(10);
            var (go, interactable) = CreateInteractable(id, Vector2.zero);
            var colliders = new[] { go.GetComponent<Collider2D>() };

            _registry.TryRegisterEntity(id, interactable, colliders);
            bool secondRegister = _registry.TryRegisterEntity(id, interactable, colliders);

            Assert.IsTrue(secondRegister);
        }

        [Test]
        public void TryRegisterEntity_RejectsConflicts()
        {
            var id1 = new EntityId(10);
            var id2 = new EntityId(20);
            var (go, interactable1) = CreateInteractable(id1, Vector2.zero);
            var (_, interactable2) = CreateInteractable(id2, Vector2.zero);
            var colliders = new[] { go.GetComponent<Collider2D>() };

            _registry.TryRegisterEntity(id1, interactable1, colliders);
            bool conflictRegister = _registry.TryRegisterEntity(id2, interactable2, colliders);

            Assert.IsFalse(conflictRegister);
        }

        [Test]
        public void TryRegisterEntity_RejectsMismatchIdOrNull()
        {
            var id1 = new EntityId(10);
            var id2 = new EntityId(20);
            var (_, interactable) = CreateInteractable(id1, Vector2.zero);

            bool nullRegister = _registry.TryRegisterEntity(id1, null, null);
            bool mismatchRegister = _registry.TryRegisterEntity(id2, interactable, null);

            Assert.IsFalse(nullRegister);
            Assert.IsFalse(mismatchRegister);
        }

        [Test]
        public void TryUnregisterEntity_ObsoleteInstanceCannotUnregisterPosterior()
        {
            var id = new EntityId(10);
            var (_, interactable1) = CreateInteractable(id, Vector2.zero);
            var (_, interactable2) = CreateInteractable(id, Vector2.zero);

            _registry.TryRegisterEntity(id, interactable1, null);
            
            // Try unregister with wrong instance
            bool unregisteredWrong = _registry.TryUnregisterEntity(id, interactable2);
            Assert.IsFalse(unregisteredWrong);

            // Correct unregister
            bool unregisteredCorrect = _registry.TryUnregisterEntity(id, interactable1);
            Assert.IsTrue(unregisteredCorrect);
        }

        private sealed class DummyInteractable : MonoBehaviour, IInteractable
        {
            public int IdValue { get; set; }
            public EntityId Id => new EntityId(IdValue);

            public bool CanInteract(in InteractionRequest request) => true;
            public InteractionResult Interact(in InteractionRequest request) => InteractionResult.Succeeded(true);
        }
    }
}
