using System.Collections.Generic;
using NUnit.Framework;

namespace Tests.EditMode.Loot
{
    public sealed class RaidLootSelectionStateTests
    {
        [Test]
        public void Selection_IsPreservedWhileStackExistsAndClearedWhenItDisappears()
        {
            var selection = new RaidLootSelectionState();
            var selectedId = new LootId("coin");
            var entries = new List<LootEntry>
            {
                new(selectedId, 3),
                new(new LootId("potion"), 1)
            };

            Assert.That(selection.TrySelect(selectedId, entries), Is.True);
            selection.Reconcile(entries);
            Assert.That(selection.SelectedLootId, Is.EqualTo(selectedId));

            entries.RemoveAt(0);
            selection.Reconcile(entries);
            Assert.That(selection.HasSelection, Is.False);
        }

        [Test]
        public void EmptyOrMissingStack_CannotBeSelected()
        {
            var selection = new RaidLootSelectionState();
            var entries = new List<LootEntry> { new(new LootId("potion"), 1) };

            Assert.That(selection.TrySelect(default, entries), Is.False);
            Assert.That(selection.TrySelect(new LootId("coin"), entries), Is.False);
            Assert.That(selection.HasSelection, Is.False);
        }
    }
}
