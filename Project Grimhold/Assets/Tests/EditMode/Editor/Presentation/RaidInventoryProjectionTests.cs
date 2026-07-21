using System.Collections.Generic;
using NUnit.Framework;

namespace Tests.EditMode.Presentation
{
    public sealed class RaidInventoryProjectionTests
    {
        [Test]
        public void TryBuild_PreservesReceivedOrderAndFillsEmptySlots()
        {
            var content = new List<LootEntry>
            {
                new(new LootId("second"), 2),
                new(new LootId("first"), 1)
            };
            var projection = new List<LootEntry>();

            bool built = RaidInventoryProjection.TryBuild(content, 4, projection);

            Assert.That(built, Is.True);
            Assert.That(projection, Has.Count.EqualTo(4));
            Assert.That(projection[0], Is.EqualTo(content[0]));
            Assert.That(projection[1], Is.EqualTo(content[1]));
            Assert.That(projection[2].IsValid, Is.False);
            Assert.That(projection[3].IsValid, Is.False);
        }

        [Test]
        public void TryBuild_EmptySnapshotProducesOnlyEmptySlots()
        {
            var projection = new List<LootEntry>();

            bool built = RaidInventoryProjection.TryBuild(
                new List<LootEntry>(),
                3,
                projection);

            Assert.That(built, Is.True);
            Assert.That(projection, Has.Count.EqualTo(3));
            Assert.That(projection[0].IsValid, Is.False);
            Assert.That(projection[1].IsValid, Is.False);
            Assert.That(projection[2].IsValid, Is.False);
        }

        [Test]
        public void TryBuild_ContentBeyondCapacityFailsWithoutPartialProjection()
        {
            var content = new List<LootEntry>
            {
                new(new LootId("one"), 1),
                new(new LootId("two"), 1)
            };
            var projection = new List<LootEntry>
            {
                new(new LootId("stale"), 1)
            };

            bool built = RaidInventoryProjection.TryBuild(content, 1, projection);

            Assert.That(built, Is.False);
            Assert.That(projection, Is.Empty);
        }
    }
}
