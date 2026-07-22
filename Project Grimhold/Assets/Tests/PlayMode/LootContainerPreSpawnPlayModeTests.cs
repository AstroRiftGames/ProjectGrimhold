#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Fusion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;

namespace Tests.PlayMode.Loot
{
    public sealed class LootContainerPreSpawnPlayModeTests
    {
        private const string LootContainerPrefabGuid = "2c19a78647c64b84a765ff0280706b7d";
        private NetworkRunner _runner;
        private NetworkObject _prefab;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return ShutdownRunner(_runner);
            _runner = null;
            _prefab = null;
        }

        [UnityTest]
        public IEnumerator AuthoritativeCallback_AppliesEmptyOverrideBeforeSpawned()
        {
            yield return StartRunnerAndLoadPrefab();

            bool callbackApplied = false;
            NetworkObject spawned = _runner.Spawn(
                _prefab,
                Vector3.zero,
                Quaternion.identity,
                inputAuthority: null,
                onBeforeSpawned: (callbackRunner, instance) =>
                {
                    NetworkLootContainer container = instance.GetComponent<NetworkLootContainer>();
                    callbackApplied = container != null &&
                        container.TrySetInitialContentOverride(
                            callbackRunner,
                            instance,
                            Array.Empty<LootEntry>());
                });

            Assert.That(callbackApplied, Is.True);
            Assert.That(spawned, Is.Not.Null);
            NetworkLootContainer spawnedContainer = spawned.GetComponent<NetworkLootContainer>();
            Assert.That((bool)spawnedContainer.IsInitialized, Is.True);
            Assert.That((bool)spawnedContainer.IsAvailable, Is.True);
            Assert.That(spawnedContainer.IsEmpty, Is.True);
            Assert.That(spawnedContainer.LootChangeSequence, Is.Zero);

            NetworkId spawnedId = spawned.Id;
            _runner.Despawn(spawned);
            Assert.That(_runner.TryFindObject(spawnedId, out _), Is.False);
        }

        [UnityTest]
        public IEnumerator RejectedCallback_FailsClosedAndCanBeCompensated()
        {
            yield return StartRunnerAndLoadPrefab();

            bool callbackApplied = true;
            LogAssert.Expect(
                UnityEngine.LogType.Error,
                "NetworkLootContainer: The requested initial-content override was rejected for 'LootContainer(Clone)'. Manual content will not be used as a fallback.");
            NetworkObject spawned = _runner.Spawn(
                _prefab,
                Vector3.zero,
                Quaternion.identity,
                inputAuthority: null,
                onBeforeSpawned: (callbackRunner, instance) =>
                {
                    NetworkLootContainer container = instance.GetComponent<NetworkLootContainer>();
                    callbackApplied = container != null &&
                        container.TrySetInitialContentOverride(
                            callbackRunner,
                            instance,
                            new[] { default(LootEntry) });
                });

            Assert.That(callbackApplied, Is.False);
            Assert.That(spawned, Is.Not.Null);
            NetworkLootContainer spawnedContainer = spawned.GetComponent<NetworkLootContainer>();
            Assert.That((bool)spawnedContainer.IsInitialized, Is.False);
            Assert.That((bool)spawnedContainer.IsAvailable, Is.False);

            NetworkId spawnedId = spawned.Id;
            _runner.Despawn(spawned);
            Assert.That(_runner.TryFindObject(spawnedId, out _), Is.False);
        }

        private IEnumerator StartRunnerAndLoadPrefab()
        {
            var runnerObject = new GameObject("LootContainerPreSpawnTestRunner");
            _runner = runnerObject.AddComponent<NetworkRunner>();
            runnerObject.AddComponent<EntityRegistry>();
            var sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();
            var objectProvider = runnerObject.AddComponent<NetworkObjectProviderDefault>();

            var startTask = _runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Single,
                SessionName = $"task35-{Guid.NewGuid():N}",
                SceneManager = sceneManager,
                ObjectProvider = objectProvider
            });
            while (!startTask.IsCompleted)
            {
                yield return null;
            }

            Assert.That(startTask.IsFaulted, Is.False, startTask.Exception?.ToString());
            Assert.That(startTask.Result.Ok, Is.True, startTask.Result.ShutdownReason.ToString());

            NetworkObjectGuid prefabGuid = NetworkObjectGuid.Parse(LootContainerPrefabGuid);
            NetworkPrefabId prefabId = _runner.Config.PrefabTable.GetId(prefabGuid);
            Assert.That(prefabId.IsValid, Is.True);
            _prefab = _runner.Config.PrefabTable.Load(prefabId, true);
            Assert.That(_prefab, Is.Not.Null);
        }

        private static IEnumerator ShutdownRunner(NetworkRunner runner)
        {
            if (runner != null && runner.IsRunning)
            {
                runner.Shutdown();
                while (runner != null && runner.IsRunning)
                {
                    yield return null;
                }
            }

            if (runner != null)
            {
                UnityEngine.Object.DestroyImmediate(runner.gameObject);
            }
        }
    }
}
#endif
