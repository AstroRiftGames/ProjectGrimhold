using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Player
{
    public class Physics2DInteractionTargetQueryTests
    {
        private GameObject _registryHolder;
        private EntityRegistry _registry;
        private GameObject _queryHolder;
        private Physics2DInteractionTargetQuery _query;
        private List<GameObject> _spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            _spawnedObjects = new List<GameObject>();

            _registryHolder = new GameObject("EntityRegistryHolder");
            _registry = _registryHolder.AddComponent<EntityRegistry>();
            _spawnedObjects.Add(_registryHolder);

            _queryHolder = new GameObject("QueryHolder");
            _query = _queryHolder.AddComponent<Physics2DInteractionTargetQuery>();
            _spawnedObjects.Add(_queryHolder);

            // Inject the registry using reflection
            typeof(Physics2DInteractionTargetQuery)
                .GetField("_registry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_query, _registry);

            // Instantiate buffer array in Physics2DInteractionTargetQuery
            typeof(Physics2DInteractionTargetQuery)
                .GetField("_colliderBuffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_query, new Collider2D[64]);
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

        private (GameObject go, DummyInteractable target) CreateTarget(EntityId id, Vector2 position, int layer)
        {
            var go = new GameObject($"Target_{id.Value}");
            go.transform.position = position;
            go.layer = layer;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.1f;

            var interactable = go.AddComponent<DummyInteractable>();
            interactable.IdValue = id.Value;

            _registry.TryRegisterEntity(id, interactable, new[] { collider });
            _spawnedObjects.Add(go);

            Physics2D.SyncTransforms();

            return (go, interactable);
        }

        [Test]
        public void FindTargets_ExcludesByLayer()
        {
            int targetLayer = LayerMask.NameToLayer("Default");
            int otherLayer = 2; // Typically ignored layer

            CreateTarget(new EntityId(2), new Vector2(1f, 0f), otherLayer);

            var query = new InteractionTargetQuery(
                new EntityId(1),
                Vector2.zero,
                2f,
                1 << targetLayer
            );

            var targets = _query.FindTargets(query);

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void FindTargets_ExcludesUnregisteredCollider()
        {
            int layer = LayerMask.NameToLayer("Default");
            var go = new GameObject("UnregisteredCollider");
            go.transform.position = new Vector2(1f, 0f);
            go.layer = layer;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.1f;
            _spawnedObjects.Add(go);

            Physics2D.SyncTransforms();

            var query = new InteractionTargetQuery(
                new EntityId(1),
                Vector2.zero,
                2f,
                1 << layer
            );

            var targets = _query.FindTargets(query);

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void FindTargets_ExcludesInteractor()
        {
            int layer = LayerMask.NameToLayer("Default");
            var interactorId = new EntityId(1);
            CreateTarget(interactorId, new Vector2(0.5f, 0f), layer);

            var query = new InteractionTargetQuery(
                interactorId,
                Vector2.zero,
                2f,
                1 << layer
            );

            var targets = _query.FindTargets(query);

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void FindTargets_MultipleCollidersFromSameTarget_ReturnsClosestAndDeduplicates()
        {
            int layer = LayerMask.NameToLayer("Default");
            var targetId = new EntityId(2);

            var go = new GameObject("MultiColliderTarget");
            go.transform.position = Vector2.zero;
            go.layer = layer;

            var col1 = go.AddComponent<CircleCollider2D>();
            col1.offset = new Vector2(0.5f, 0f);
            col1.radius = 0.1f;

            var col2 = go.AddComponent<CircleCollider2D>();
            col2.offset = new Vector2(1.2f, 0f);
            col2.radius = 0.1f;

            var interactable = go.AddComponent<DummyInteractable>();
            interactable.IdValue = targetId.Value;

            _registry.TryRegisterEntity(targetId, interactable, new[] { col1, col2 });
            _spawnedObjects.Add(go);

            Physics2D.SyncTransforms();

            var query = new InteractionTargetQuery(
                new EntityId(1),
                Vector2.zero,
                2f,
                1 << layer
            );

            var targets = _query.FindTargets(query);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(targetId, targets[0].TargetId);
            Assert.IsTrue(targets[0].Distance < 0.6f);
        }

        [Test]
        public void FindTargets_OrdersByDistanceAndIDTieBreaker()
        {
            int layer = LayerMask.NameToLayer("Default");

            CreateTarget(new EntityId(3), new Vector2(1.5f, 0f), layer);
            CreateTarget(new EntityId(2), new Vector2(0.8f, 0f), layer);

            var query = new InteractionTargetQuery(
                new EntityId(1),
                Vector2.zero,
                2f,
                1 << layer
            );

            var targets = _query.FindTargets(query);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(new EntityId(2), targets[0].TargetId);
            Assert.AreEqual(new EntityId(3), targets[1].TargetId);
        }

        [Test]
        public void FindTargets_ExcludesTargetsOutOfRange()
        {
            int layer = LayerMask.NameToLayer("Default");

            CreateTarget(new EntityId(2), new Vector2(3f, 0f), layer);

            var query = new InteractionTargetQuery(
                new EntityId(1),
                Vector2.zero,
                2f,
                1 << layer
            );

            var targets = _query.FindTargets(query);

            Assert.AreEqual(0, targets.Count);
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
