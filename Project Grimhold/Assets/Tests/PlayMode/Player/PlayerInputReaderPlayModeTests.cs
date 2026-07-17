using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Reflection;

namespace Tests.PlayMode.Player
{
    public class PlayerInputReaderPlayModeTests
    {
        private GameObject _holder;
        private PlayerInputReader _reader;

        [SetUp]
        public void SetUp()
        {
            var old = GameObject.Find("PlayerInputReaderHolder");
            if (old != null)
            {
                Object.DestroyImmediate(old);
            }

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

        [UnityTest]
        public IEnumerator OnDisable_ClearsPendingInteract_PlayMode()
        {
            var method = typeof(PlayerInputReader).GetMethod("OnInteractPerformed", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);

            // 1. Generate Interact
            method.Invoke(_reader, new object[] { default(UnityEngine.InputSystem.InputAction.CallbackContext) });

            // 2. Confirm that the first consume contains Interact
            PlayerNetworkInput result1 = _reader.ConsumeNetworkInput();
            Assert.IsTrue(result1.Buttons.IsSet(PlayerInputButton.Interact), "First consume should contain Interact");

            // 3. Generate another pending interaction
            method.Invoke(_reader, new object[] { default(UnityEngine.InputSystem.InputAction.CallbackContext) });

            // 4. Disable the component
            _reader.enabled = false;
            yield return null;

            // 5. Reactivate the component
            _reader.enabled = true;
            yield return null;

            // 6. Consume input and check that Interact is false
            PlayerNetworkInput result2 = _reader.ConsumeNetworkInput();
            Assert.IsFalse(result2.Buttons.IsSet(PlayerInputButton.Interact), "Pending interact should be cleared on disable");

            // 7. Verify that a new press after reactivation registers Interact = true
            method.Invoke(_reader, new object[] { default(UnityEngine.InputSystem.InputAction.CallbackContext) });
            PlayerNetworkInput result3 = _reader.ConsumeNetworkInput();
            Assert.IsTrue(result3.Buttons.IsSet(PlayerInputButton.Interact), "New press after reactivation should be registered");
        }
    }
}
