using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Loot
{
    public sealed class PlayerLootReceiverTests
    {
        [Test]
        public void LootEntry_ConservesImmutableReadData()
        {
            var lootId = new LootId("coin");
            var entry = new LootEntry(lootId, 12);

            Assert.That(entry.LootId, Is.EqualTo(lootId));
            Assert.That(entry.Amount, Is.EqualTo(12));
            Assert.That(entry.IsValid, Is.True);
        }

        [Test]
        public void MaxLootTypes_MatchesNetworkDictionaryCapacityContract()
        {
            Assert.That(PlayerLootReceiver.MaxLootTypes, Is.EqualTo(64));
        }

        [Test]
        public void DefaultSlotCapacity_IsExplicitGameplayConfiguration()
        {
            var holder = new GameObject(nameof(DefaultSlotCapacity_IsExplicitGameplayConfiguration));
            var receiver = holder.AddComponent<PlayerLootReceiver>();

            Assert.That(receiver.SlotCapacity, Is.EqualTo(16));

            Object.DestroyImmediate(holder);
        }

        [Test]
        public void LootGrantPresentationEvent_ConservesAuthoritativeDeliveryData()
        {
            var sourceId = new EntityId(10);
            var receiverId = new EntityId(20);
            var lootId = new LootId("coin");

            var presentationEvent = new LootGrantPresentationEvent(
                4,
                sourceId,
                receiverId,
                lootId,
                3,
                120);

            Assert.That(presentationEvent.Sequence, Is.EqualTo(4));
            Assert.That(presentationEvent.SourceId, Is.EqualTo(sourceId));
            Assert.That(presentationEvent.ReceiverId, Is.EqualTo(receiverId));
            Assert.That(presentationEvent.LootId, Is.EqualTo(lootId));
            Assert.That(presentationEvent.Amount, Is.EqualTo(3));
            Assert.That(presentationEvent.SimulationTick, Is.EqualTo(120));
        }

        [Test]
        public void InteractionPresentationEvent_ConservesSequenceAndTypedFailure()
        {
            var presentationEvent = new InteractionPresentationEvent(
                7,
                new EntityId(1),
                default,
                121,
                false,
                false,
                InteractionFailureReason.InteractionDisabled);

            Assert.That(presentationEvent.Sequence, Is.EqualTo(7));
            Assert.That(presentationEvent.Success, Is.False);
            Assert.That(presentationEvent.FailureReason, Is.EqualTo(InteractionFailureReason.InteractionDisabled));
        }
    }
}
