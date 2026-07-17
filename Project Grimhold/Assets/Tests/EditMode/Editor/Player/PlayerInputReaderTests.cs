using NUnit.Framework;
using UnityEngine;
using Fusion;
using System.Reflection;
using Assert = NUnit.Framework.Assert;

namespace Tests.EditMode.Player
{
    public class PlayerInputReaderTests
    {
        private GameObject _holder;
        private PlayerInputReader _reader;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("PlayerInputReaderHolder");
            _reader = _holder.AddComponent<PlayerInputReader>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {fieldName} not found.");
            field.SetValue(target, value);
        }

        [Test]
        public void ConsumeNetworkInput_PreservesAimAndMovementData()
        {
            Vector2 testMove = new Vector2(0.5f, -0.8f);
            Vector2 testAim = new Vector2(10.5f, 20.3f);
            NetworkButtons testButtons = default;
            testButtons.Set(PlayerInputButton.PrimaryAttack, true);

            SetPrivateField(_reader, "_moveDirection", testMove);
            SetPrivateField(_reader, "_aimWorldPosition", testAim);
            SetPrivateField(_reader, "_buttons", testButtons);

            PlayerNetworkInput result = _reader.ConsumeNetworkInput();

            Assert.AreEqual(testMove, result.MoveDirection);
            Assert.AreEqual(testAim, result.AimWorldPosition);
            Assert.IsTrue(result.Buttons.IsSet(PlayerInputButton.PrimaryAttack));
        }

        [Test]
        public void ConsumeNetworkInput_InteractLatchingAndClearing()
        {
            var method = typeof(PlayerInputReader).GetMethod("OnInteractPerformed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);

            method.Invoke(_reader, new object[] { default(UnityEngine.InputSystem.InputAction.CallbackContext) });

            PlayerNetworkInput result1 = _reader.ConsumeNetworkInput();
            Assert.IsTrue(result1.Buttons.IsSet(PlayerInputButton.Interact), "Interact should be set in the first consume");

            PlayerNetworkInput result2 = _reader.ConsumeNetworkInput();
            Assert.IsFalse(result2.Buttons.IsSet(PlayerInputButton.Interact), "Interact should be cleared in the second consume");
        }
    }
}
