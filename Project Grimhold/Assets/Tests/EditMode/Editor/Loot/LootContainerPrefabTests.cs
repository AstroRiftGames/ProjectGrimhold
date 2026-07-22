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
            NetworkLootContainer container = prefab.GetComponent<NetworkLootContainer>();
            NetworkLootContainerInteractable interactable = prefab.GetComponent<NetworkLootContainerInteractable>();
            InteractionPromptMetadata metadata = prefab.GetComponent<InteractionPromptMetadata>();
            LootContainerRandomContentConfig randomConfig = prefab.GetComponent<LootContainerRandomContentConfig>();
            Assert.That(container, Is.Not.Null);
            Assert.That(interactable, Is.Not.Null);
            Assert.That(metadata, Is.Not.Null);
            Assert.That(randomConfig, Is.Not.Null);
            Assert.That(randomConfig.enabled, Is.True);
            Assert.That(randomConfig.Table, Is.Not.Null);
            Assert.That(metadata.PromptText, Is.EqualTo("Abrir cofre"));
            Assert.That(container.gameObject, Is.SameAs(interactable.gameObject));
            Assert.That(prefab.GetComponent<Collider2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<SpriteRenderer>(true), Is.Not.Null);
            Assert.That(prefab.GetComponent<IInteractable>(), Is.SameAs(interactable));
            Assert.That(prefab.GetComponent<ILootReceiver>(), Is.Null);
        }

        [Test]
        public void CorpsePrefab_ReusesContainerAndAdapterWithoutCorpseGameplayScripts()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LootCorpse.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<NetworkObject>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<NetworkObject>(true), Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponent<NetworkLootContainer>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<NetworkLootContainerInteractable>(), Is.Not.Null);
            LootContainerRandomContentConfig randomConfig = prefab.GetComponent<LootContainerRandomContentConfig>();
            Assert.That(randomConfig, Is.Not.Null);
            Assert.That(randomConfig.enabled, Is.False);
            Assert.That(randomConfig.Table, Is.Null);
            Assert.That(prefab.GetComponent<InteractionPromptMetadata>().PromptText, Is.EqualTo("Saquear cadáver"));
            Assert.That(prefab.GetComponent<IDamageable>(), Is.Null);
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
