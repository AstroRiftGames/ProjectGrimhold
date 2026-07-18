using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Loot
{
    public class LootDefinitionCatalogTests
    {
        private LootDefinitionCatalog _catalog;
        private List<LootDefinition> _createdDefinitions;
        private Sprite _testSprite;
        private Texture2D _testTexture;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<LootDefinitionCatalog>();
            _createdDefinitions = new List<LootDefinition>();
            _testTexture = new Texture2D(8, 8);
            _testSprite = Sprite.Create(_testTexture, new Rect(0, 0, 8, 8), Vector2.zero);
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalog != null)
            {
                Object.DestroyImmediate(_catalog);
            }

            foreach (var definition in _createdDefinitions)
            {
                if (definition != null)
                {
                    Object.DestroyImmediate(definition);
                }
            }

            if (_testSprite != null)
            {
                Object.DestroyImmediate(_testSprite);
            }

            if (_testTexture != null)
            {
                Object.DestroyImmediate(_testTexture);
            }
        }

        private LootDefinition CreateValidDefinition(string id)
        {
            var def = ScriptableObject.CreateInstance<LootDefinition>();
            _createdDefinitions.Add(def);

            FieldInfo idField = typeof(LootDefinition).GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo nameField = typeof(LootDefinition).GetField("_displayName", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo iconField = typeof(LootDefinition).GetField("_icon", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo worldSpriteField = typeof(LootDefinition).GetField("_worldSprite", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo categoryField = typeof(LootDefinition).GetField("_category", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo rarityField = typeof(LootDefinition).GetField("_rarity", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo valueField = typeof(LootDefinition).GetField("_extractionValuePerUnit", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo qtyField = typeof(LootDefinition).GetField("_defaultPickupQuantity", BindingFlags.Instance | BindingFlags.NonPublic);

            idField.SetValue(def, id);
            nameField.SetValue(def, "Test Item " + id);
            iconField.SetValue(def, _testSprite);
            worldSpriteField.SetValue(def, _testSprite);
            categoryField.SetValue(def, LootCategory.Valuable);
            rarityField.SetValue(def, LootRarity.Common);
            valueField.SetValue(def, 10);
            qtyField.SetValue(def, 1);

            return def;
        }

        private void SetCatalogDefinitions(params LootDefinition[] definitions)
        {
            FieldInfo field = typeof(LootDefinitionCatalog).GetField("_definitions", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Field _definitions not found on LootDefinitionCatalog.");
            field.SetValue(_catalog, new List<LootDefinition>(definitions));

            FieldInfo dirtyField = typeof(LootDefinitionCatalog).GetField("_isCacheDirty", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(dirtyField, Is.Not.Null, "Field _isCacheDirty not found on LootDefinitionCatalog.");
            dirtyField.SetValue(_catalog, true);
        }

        [Test]
        public void EmptyCatalog_FailsValidation()
        {
            SetCatalogDefinitions();
            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("no entries"));
        }

        [Test]
        public void CatalogWithNullEntry_FailsValidation()
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            SetCatalogDefinitions(d1, null);
            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("null definition reference"));
        }

        [Test]
        public void CatalogWithInvalidDefinition_FailsValidation()
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            // Set invalid ID on d1
            FieldInfo idField = typeof(LootDefinition).GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic);
            idField.SetValue(d1, ""); // empty ID is invalid

            SetCatalogDefinitions(d1);
            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("invalid definition"));
        }

        [Test]
        public void CatalogWithDuplicateIds_FailsValidation()
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            LootDefinition d2 = CreateValidDefinition("coin"); // same ID, different reference

            SetCatalogDefinitions(d1, d2);
            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("duplicate entry for loot ID"));
        }

        [Test]
        public void CatalogWithDuplicateReferences_FailsValidation()
        {
            LootDefinition d1 = CreateValidDefinition("coin");

            SetCatalogDefinitions(d1, d1); // same reference added twice
            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("duplicate reference for loot definition"));
        }

        [Test]
        public void ValidCatalog_PassesValidation()
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            LootDefinition d2 = CreateValidDefinition("gem");

            SetCatalogDefinitions(d1, d2);
            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.True, $"Valid catalog should pass but failed: {error}");
            Assert.That(error, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        public void TryGet_NullOrEmptyId_ReturnsFalse(string invalidId)
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            SetCatalogDefinitions(d1);

            bool found = _catalog.TryGet(invalidId, out LootDefinition result);
            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryGet_NonExistentId_ReturnsFalse()
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            SetCatalogDefinitions(d1);

            bool found = _catalog.TryGet("gem", out LootDefinition result);
            Assert.That(found, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryGet_ExistingId_ReturnsDefinition()
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            LootDefinition d2 = CreateValidDefinition("gem");
            SetCatalogDefinitions(d1, d2);

            bool found = _catalog.TryGet("gem", out LootDefinition result);
            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(d2));
        }

        [Test]
        public void TryGet_UsesOrdinalComparison()
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            SetCatalogDefinitions(d1);

            // "COIN" or "Coin" should not match since lookup is exact Ordinal comparison.
            bool foundUpper = _catalog.TryGet("COIN", out LootDefinition resultUpper);
            Assert.That(foundUpper, Is.False);
            Assert.That(resultUpper, Is.Null);
        }

        [Test]
        public void RebuildsCacheAfterModification()
        {
            LootDefinition d1 = CreateValidDefinition("coin");
            SetCatalogDefinitions(d1);

            // Verify first lookup succeeds
            bool found1 = _catalog.TryGet("coin", out LootDefinition result1);
            Assert.That(found1, Is.True);
            Assert.That(result1, Is.SameAs(d1));

            // Now modify definitions dynamically and mark dirty
            LootDefinition d2 = CreateValidDefinition("gem");
            SetCatalogDefinitions(d1, d2);

            // Lookup new definition
            bool found2 = _catalog.TryGet("gem", out LootDefinition result2);
            Assert.That(found2, Is.True);
            Assert.That(result2, Is.SameAs(d2));
        }
    }
}
