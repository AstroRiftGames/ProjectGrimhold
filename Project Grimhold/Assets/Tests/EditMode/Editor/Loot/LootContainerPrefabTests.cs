using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using NetworkObject = Fusion.NetworkObject;

namespace Tests.EditMode.Loot
{
    public sealed class LootContainerPrefabTests
    {
        [Test]
        public void PlayerPrefab_ContainsTransferControllerButNoDebugHarness()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NetworkPlayer.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerLootTransferNetworkController>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<LootContainerTransferDebugHarness>(true), Is.Null);
        }

        [Test]
        public void ContainerPrefab_HasRequiredProductionComponentsAndLayer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LootContainer.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.layer, Is.EqualTo(8));
            Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<NetworkLootContainer>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Collider2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(true), Is.Not.Null);
            Assert.That(prefab.GetComponent<IInteractable>(), Is.Null);
            Assert.That(prefab.GetComponent<ILootReceiver>(), Is.Null);
        }

        [Test]
        public void DebugHarnessPrefab_IsSeparateAndHasNoNetworkGameplayEndpoint()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Debug/LootContainerTransferDebugHarness.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<LootContainerTransferDebugHarness>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<NetworkObject>(), Is.Null);
            Assert.That(prefab.GetComponent<PlayerLootReceiver>(), Is.Null);
        }
    }
}
