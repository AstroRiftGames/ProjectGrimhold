using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Reflection;
using Assert = NUnit.Framework.Assert;

namespace Tests.EditMode.Player
{
    public class PlayerClassCatalogTests
    {
        private PlayerClassCatalog _catalog;

        private static readonly FieldInfo EntriesField =
            typeof(PlayerClassCatalog).GetField(
                "_entries",
                BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<PlayerClassCatalog>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalog != null)
            {
                Object.DestroyImmediate(_catalog);
            }
        }

        private static void SetEntries(PlayerClassCatalog catalog, params PlayerClassCatalog.ClassEntry[] entries)
        {
            Assert.That(
                EntriesField,
                Is.Not.Null,
                "PlayerClassCatalog._entries field was not found.");

            EntriesField.SetValue(
                catalog,
                new List<PlayerClassCatalog.ClassEntry>(entries));
        }

        private static PlayerClassCatalog LoadConfiguredCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:PlayerClassCatalog");
            Assert.That(
                guids,
                Is.Not.Empty,
                "No PlayerClassCatalog asset was found.");

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            PlayerClassCatalog catalog = AssetDatabase.LoadAssetAtPath<PlayerClassCatalog>(path);
            Assert.That(
                catalog,
                Is.Not.Null,
                $"Could not load PlayerClassCatalog at {path}.");

            return catalog;
        }

        [Test]
        public void TryValidate_EmptyCatalog_Fails()
        {
            SetEntries(_catalog);
            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(error, Does.Contain("no entries"));
        }

        [Test]
        public void TryValidate_ContainsNoneClass_Fails()
        {
            SetEntries(
                _catalog,
                new PlayerClassCatalog.ClassEntry
                {
                    ClassId = PlayerClassId.None,
                    Prefab = default
                });

            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(error, Does.Contain("None"));
        }

        [Test]
        public void TryValidate_ContainsUnsupportedOrUnknownClass_Fails()
        {
            SetEntries(
                _catalog,
                new PlayerClassCatalog.ClassEntry
                {
                    ClassId = (PlayerClassId)255,
                    Prefab = default
                });

            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(error, Does.Contain("unsupported"));
        }

        [Test]
        public void TryValidate_ContainsInvalidPrefab_Fails()
        {
            SetEntries(
                _catalog,
                new PlayerClassCatalog.ClassEntry
                {
                    ClassId = PlayerClassId.Melee,
                    Prefab = default
                });

            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(error, Does.Contain("invalid prefab"));
        }

        [Test]
        public void TryValidate_ContainsDuplicateClass_Fails()
        {
            // Load real catalog to borrow a valid NetworkPrefabRef
            PlayerClassCatalog realCatalog = LoadConfiguredCatalog();
            bool hasPrefab = realCatalog.TryGetPrefab(PlayerClassId.Melee, out NetworkPrefabRef validPrefab);
            Assert.That(hasPrefab, Is.True, "Real catalog must contain a valid Melee prefab for duplicate test.");

            SetEntries(
                _catalog,
                new PlayerClassCatalog.ClassEntry { ClassId = PlayerClassId.Melee, Prefab = validPrefab },
                new PlayerClassCatalog.ClassEntry { ClassId = PlayerClassId.Melee, Prefab = validPrefab }
            );

            bool result = _catalog.TryValidate(out string error);
            Assert.That(result, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(error, Does.Contain("duplicate"));
        }

        [Test]
        public void TryGetPrefab_ClassNotRegistered_Fails()
        {
            SetEntries(
                _catalog,
                new PlayerClassCatalog.ClassEntry
                {
                    ClassId = PlayerClassId.Melee,
                    Prefab = default
                });

            bool result = _catalog.TryGetPrefab(PlayerClassId.Ranged, out NetworkPrefabRef prefab);
            Assert.That(result, Is.False);
            Assert.That(prefab.IsValid, Is.False);
        }

        [Test]
        public void TryGetPrefab_UnsupportedClassQuery_Fails()
        {
            SetEntries(
                _catalog,
                new PlayerClassCatalog.ClassEntry
                {
                    ClassId = PlayerClassId.Melee,
                    Prefab = default
                });

            bool result = _catalog.TryGetPrefab((PlayerClassId)255, out NetworkPrefabRef prefab);
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryValidate_ValidCatalog_Succeeds()
        {
            PlayerClassCatalog catalog = LoadConfiguredCatalog();
            bool result = catalog.TryValidate(out string error);
            Assert.That(result, Is.True);
            Assert.That(error, Is.Null);
        }

        [TestCase(PlayerClassId.Melee)]
        [TestCase(PlayerClassId.Ranged)]
        public void ConfiguredCatalog_ContainsValidClass(PlayerClassId classId)
        {
            PlayerClassCatalog catalog = LoadConfiguredCatalog();
            bool result = catalog.TryGetPrefab(classId, out NetworkPrefabRef prefab);
            Assert.That(result, Is.True);
            Assert.That(prefab.IsValid, Is.True);
        }
    }
}
