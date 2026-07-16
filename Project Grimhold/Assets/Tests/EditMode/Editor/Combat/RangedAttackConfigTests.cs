using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Fusion;
using System.Reflection;
using Assert = NUnit.Framework.Assert;

namespace Tests.EditMode.Combat
{
    public class RangedAttackConfigTests
    {
        private RangedAttackConfig _config;

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }
        }

        private static RangedAttackConfig CreateValidConfigClone()
        {
            const string AssetPath = "Assets/ScriptableObjects/RangedAttackConfig.asset";
            RangedAttackConfig source = AssetDatabase.LoadAssetAtPath<RangedAttackConfig>(AssetPath);
            Assert.That(source, Is.Not.Null, $"Could not load valid config at {AssetPath}.");
            return Object.Instantiate(source);
        }

        private static void SetPrivateField<T>(object target, System.Type declaringType, string fieldName, T value)
        {
            FieldInfo field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {declaringType.Name}.{fieldName} was not found.");
            field.SetValue(target, value);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            SetPrivateField(target, target.GetType(), fieldName, value);
        }

        [Test]
        public void TryValidate_WithInvalidDamage_Fails()
        {
            _config = CreateValidConfigClone();
            SetPrivateField(_config, typeof(AttackConfig), "_damage", 0f);

            bool result = _config.TryValidate(out string error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("Damage"));
        }

        [Test]
        public void TryValidate_WithInvalidSpeed_Fails()
        {
            _config = CreateValidConfigClone();
            SetPrivateField(_config, "_projectileSpeed", 0f);

            bool result = _config.TryValidate(out string error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("ProjectileSpeed"));
        }

        [Test]
        public void TryValidate_WithInvalidLifetime_Fails()
        {
            _config = CreateValidConfigClone();
            SetPrivateField(_config, "_lifetimeSeconds", -1f);

            bool result = _config.TryValidate(out string error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("LifetimeSeconds"));
        }

        [Test]
        public void TryValidate_WithInvalidRange_Fails()
        {
            _config = CreateValidConfigClone();
            SetPrivateField(_config, "_maxRange", 0f);

            bool result = _config.TryValidate(out string error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("MaxRange"));
        }

        [Test]
        public void TryValidate_WithNegativeOffset_Fails()
        {
            _config = CreateValidConfigClone();
            SetPrivateField(_config, "_projectileSpawnOffset", -0.5f);

            bool result = _config.TryValidate(out string error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("ProjectileSpawnOffset"));
        }

        [Test]
        public void TryValidate_WithEmptyLayerMask_Fails()
        {
            _config = CreateValidConfigClone();
            SetPrivateField(_config, "_impactLayerMask", new LayerMask { value = 0 });

            bool result = _config.TryValidate(out string error);
            Assert.IsFalse(result);
            Assert.That(error, Does.Contain("ImpactLayerMask"));
        }

        [Test]
        public void TryValidate_WithRealConfigAsset_Passes()
        {
            _config = CreateValidConfigClone();
            bool result = _config.TryValidate(out string error);
            Assert.IsTrue(result, _config.name + " " + error);
        }
    }
}
