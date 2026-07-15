using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Combat
{
    public class Physics2DAttackTargetQueryTests
    {
        private GameObject _registryHolder;
        private EntityRegistry _registry;
        private GameObject _queryHolder;
        private Physics2DAttackTargetQuery _query;
        private List<GameObject> _spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            _spawnedObjects = new List<GameObject>();
            
            _registryHolder = new GameObject("EntityRegistryHolder");
            _registry = _registryHolder.AddComponent<EntityRegistry>();
            _spawnedObjects.Add(_registryHolder);

            _queryHolder = new GameObject("QueryHolder");
            _query = _queryHolder.AddComponent<Physics2DAttackTargetQuery>();
            _spawnedObjects.Add(_queryHolder);

            // Inject the registry using reflection
            typeof(Physics2DAttackTargetQuery)
                .GetField("_registry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_query, _registry);

            // Instantiate buffer array in Physics2DAttackTargetQuery
            typeof(Physics2DAttackTargetQuery)
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

        private (GameObject go, DummyCharacter target) CreateTarget(EntityId id, Vector2 position, int layer, bool isAlive = true, bool canReceiveDamage = true)
        {
            var go = new GameObject($"Target_{id.Value}");
            go.transform.position = position;
            go.layer = layer;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.1f;

            var character = go.AddComponent<DummyCharacter>();
            character.Id = id;
            character.IsAlive = isAlive;
            character.CanReceiveDamage = canReceiveDamage;

            _registry.TryRegister(id, character, new[] { collider });
            _spawnedObjects.Add(go);

            Physics2D.SyncTransforms();

            return (go, character);
        }

        [Test]
        public void FindTargets_ExcludesByLayer()
        {
            int targetLayer = LayerMask.NameToLayer("Default");
            int otherLayer = 2; // Typically ignored layer

            CreateTarget(new EntityId(2), new Vector2(1f, 0f), otherLayer);

            var request = new AttackTargetQuery(
                new EntityId(1),
                Vector2.zero,
                Vector2.right,
                1f,
                1f,
                5,
                1 << targetLayer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void FindTargets_ExcludesColliderWithoutEntity()
        {
            int layer = LayerMask.NameToLayer("Default");
            var go = new GameObject("UnregisteredCollider");
            go.transform.position = new Vector2(1f, 0f);
            go.layer = layer;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.1f;
            _spawnedObjects.Add(go);

            Physics2D.SyncTransforms();

            var request = new AttackTargetQuery(
                new EntityId(1),
                Vector2.zero,
                Vector2.right,
                1f,
                1f,
                5,
                1 << layer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void FindTargets_ExcludesNonDamageableEntity()
        {
            int layer = LayerMask.NameToLayer("Default");
            CreateTarget(new EntityId(2), new Vector2(1f, 0f), layer, canReceiveDamage: false);

            var request = new AttackTargetQuery(
                new EntityId(1),
                Vector2.zero,
                Vector2.right,
                1f,
                1f,
                5,
                1 << layer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void FindTargets_ExcludesDeadEntity()
        {
            int layer = LayerMask.NameToLayer("Default");
            CreateTarget(new EntityId(2), new Vector2(1f, 0f), layer, isAlive: false);

            var request = new AttackTargetQuery(
                new EntityId(1),
                Vector2.zero,
                Vector2.right,
                1f,
                1f,
                5,
                1 << layer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void FindTargets_ExcludesAttacker()
        {
            int layer = LayerMask.NameToLayer("Default");
            var attackerId = new EntityId(1);
            CreateTarget(attackerId, new Vector2(1f, 0f), layer);

            var request = new AttackTargetQuery(
                attackerId,
                Vector2.zero,
                Vector2.right,
                1f,
                1f,
                5,
                1 << layer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(0, targets.Count);
        }

        [Test]
        public void FindTargets_MultipleCollidersFromSameTarget_ReturnsClosestHitAndDeduplicates()
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

            var character = go.AddComponent<DummyCharacter>();
            character.Id = targetId;
            character.IsAlive = true;
            character.CanReceiveDamage = true;

            _registry.TryRegister(targetId, character, new[] { col1, col2 });
            _spawnedObjects.Add(go);

            Physics2D.SyncTransforms();

            var request = new AttackTargetQuery(
                new EntityId(1),
                Vector2.zero,
                Vector2.right,
                1f,
                1.5f,
                5,
                1 << layer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(targetId, targets[0].TargetId);
            Assert.IsTrue(targets[0].HitPoint.x < 0.6f);
        }

        [Test]
        public void FindTargets_OrdersByDistanceToOrigin()
        {
            int layer = LayerMask.NameToLayer("Default");
            
            CreateTarget(new EntityId(3), new Vector2(1.3f, 0f), layer);
            CreateTarget(new EntityId(2), new Vector2(0.8f, 0f), layer);

            var request = new AttackTargetQuery(
                new EntityId(1),
                Vector2.zero,
                Vector2.right,
                1f,
                1f,
                5,
                1 << layer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(new EntityId(2), targets[0].TargetId);
            Assert.AreEqual(new EntityId(3), targets[1].TargetId);
        }

        [Test]
        public void FindTargets_TieBreakerByEntityId()
        {
            int layer = LayerMask.NameToLayer("Default");
            
            CreateTarget(new EntityId(3), new Vector2(1f, 1f), layer);
            CreateTarget(new EntityId(2), new Vector2(1f, -1f), layer);

            var request = new AttackTargetQuery(
                new EntityId(1),
                Vector2.zero,
                Vector2.right,
                1f,
                2f,
                5,
                1 << layer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(new EntityId(2), targets[0].TargetId);
            Assert.AreEqual(new EntityId(3), targets[1].TargetId);
        }

        [Test]
        public void FindTargets_AppliesMaximumTargetsAfterDeduplication()
        {
            int layer = LayerMask.NameToLayer("Default");

            var goA = new GameObject("TargetA");
            goA.transform.position = Vector2.zero;
            goA.layer = layer;

            var colA1 = goA.AddComponent<CircleCollider2D>();
            colA1.offset = new Vector2(0.5f, 0f);

            var colA2 = goA.AddComponent<CircleCollider2D>();
            colA2.offset = new Vector2(0.6f, 0f);

            var charA = goA.AddComponent<DummyCharacter>();
            charA.Id = new EntityId(2);
            charA.IsAlive = true;
            charA.CanReceiveDamage = true;

            _registry.TryRegister(charA.Id, charA, new[] { colA1, colA2 });
            _spawnedObjects.Add(goA);

            CreateTarget(new EntityId(3), new Vector2(0.9f, 0f), layer);

            Physics2D.SyncTransforms();

            var request = new AttackTargetQuery(
                new EntityId(1),
                Vector2.zero,
                Vector2.right,
                1f,
                1.5f,
                2,
                1 << layer
            );

            var targets = _query.FindTargets(request);

            Assert.AreEqual(2, targets.Count);
            Assert.AreEqual(new EntityId(2), targets[0].TargetId);
            Assert.AreEqual(new EntityId(3), targets[1].TargetId);
        }

        private sealed class DummyCharacter : MonoBehaviour, IDamageable, ICharacter
        {
            public EntityId Id { get; set; }
            public bool IsAlive { get; set; }
            public bool CanReceiveDamage { get; set; }

            public DamageResult ApplyDamage(in DamageRequest request)
            {
                return new DamageResult(Id, true, request.Amount, 100f, false, DamageFailureReason.None);
            }
        }
    }
}