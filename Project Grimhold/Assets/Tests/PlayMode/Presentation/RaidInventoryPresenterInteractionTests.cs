#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Assert = NUnit.Framework.Assert;

namespace Tests.PlayMode.Presentation
{
    public sealed class RaidInventoryPresenterInteractionTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/NetworkPlayer.prefab";
        private const string ChestPrefabPath = "Assets/Prefabs/LootContainer.prefab";
        private const string CorpsePrefabPath = "Assets/Prefabs/LootCorpse.prefab";

        private GameObject _playerInstance;
        private GameObject _chestInstance;
        private GameObject _corpseInstance;
        private PlayerInputReader _inputReader;
        private RaidInventoryPresenter _presenter;
        private RaidInventoryView _view;
        private Keyboard _keyboard;
        private object _inputTestFixture;
        private Type _inputTestFixtureType;

        [SetUp]
        public void SetUp()
        {
            _inputTestFixtureType = Type.GetType(
                "UnityEngine.InputSystem.InputTestFixture, Unity.InputSystem.TestFramework",
                true);
            _inputTestFixture = Activator.CreateInstance(_inputTestFixtureType);
            _inputTestFixtureType.GetMethod("Setup").Invoke(_inputTestFixture, null);

            _keyboard = InputSystem.AddDevice<Keyboard>();

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(playerPrefab, Is.Not.Null);

            _playerInstance = UnityEngine.Object.Instantiate(playerPrefab);
            _playerInstance.SetActive(false);

            _inputReader = _playerInstance.GetComponentInChildren<PlayerInputReader>(true);
            _presenter = _playerInstance.GetComponentInChildren<RaidInventoryPresenter>(true);
            _view = _playerInstance.GetComponentInChildren<RaidInventoryView>(true);

            Assert.That(_inputReader, Is.Not.Null);
            Assert.That(_presenter, Is.Not.Null);
            Assert.That(_view, Is.Not.Null);

            PlayerInputActions actions = ReadInputActions(_inputReader);
            actions.asset.devices = new InputDevice[] { _keyboard };
            EnsureInputEnabled(_inputReader);

            _chestInstance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ChestPrefabPath));
            _chestInstance.SetActive(false);

            _corpseInstance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CorpsePrefabPath));
            _corpseInstance.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerInstance != null) UnityEngine.Object.DestroyImmediate(_playerInstance);
            if (_chestInstance != null) UnityEngine.Object.DestroyImmediate(_chestInstance);
            if (_corpseInstance != null) UnityEngine.Object.DestroyImmediate(_corpseInstance);

            InputSystem.RemoveDevice(_keyboard);
            _inputTestFixtureType.GetMethod("TearDown").Invoke(_inputTestFixture, null);
        }

        [Test]
        public void LocalInteractPress_WhileInContainerLootMode_ClosesScreenAndReleasesSuppression()
        {
            SetPresenterMode(_presenter, 2); // ScreenMode.ContainerLoot
            SetViewScreenVisible(_view, true);
            SetViewContainerVisible(_view, true);
            IDisposable suppression = _inputReader.AcquireGameplayInputSuppression();
            SetPresenterField(_presenter, "_inputSuppression", suppression);
            SetPresenterField(_presenter, "_inputReader", _inputReader);

            InvokeMethod(_presenter, "Subscribe");

            SetKey(_keyboard, Key.E, true);

            Assert.That(GetPresenterMode(_presenter), Is.EqualTo(0)); // ScreenMode.Closed
            Assert.That(_view.IsOpen, Is.False);
            Assert.That(ReadSuppressionCount(_inputReader), Is.EqualTo(0));
        }

        [Test]
        public void LocalInteractPress_WhileInPersonalMode_DoesNotCloseOrChangeMode()
        {
            SetPresenterMode(_presenter, 1); // ScreenMode.Personal
            SetViewScreenVisible(_view, true);
            IDisposable suppression = _inputReader.AcquireGameplayInputSuppression();
            SetPresenterField(_presenter, "_inputSuppression", suppression);
            SetPresenterField(_presenter, "_inputReader", _inputReader);

            InvokeMethod(_presenter, "Subscribe");

            SetKey(_keyboard, Key.E, true);

            Assert.That(GetPresenterMode(_presenter), Is.EqualTo(1)); // ScreenMode.Personal
            Assert.That(_view.IsOpen, Is.True);
            Assert.That(ReadSuppressionCount(_inputReader), Is.EqualTo(1));

            suppression.Dispose();
        }

        [Test]
        public void ClosingLootMode_DoesNotModifyContainerContentOrAvailability()
        {
            NetworkLootContainer chestContainer = _chestInstance.GetComponent<NetworkLootContainer>();
            NetworkLootContainerInteractable chestInteractable = _chestInstance.GetComponent<NetworkLootContainerInteractable>();
            Assert.That(chestContainer, Is.Not.Null);
            Assert.That(chestInteractable, Is.Not.Null);

            SetPresenterMode(_presenter, 2);
            SetPresenterField(_presenter, "_container", chestContainer);
            SetPresenterField(_presenter, "_containerInteractable", chestInteractable);
            IDisposable suppression = _inputReader.AcquireGameplayInputSuppression();
            SetPresenterField(_presenter, "_inputSuppression", suppression);
            SetPresenterField(_presenter, "_inputReader", _inputReader);

            InvokeMethod(_presenter, "Subscribe");

            SetKey(_keyboard, Key.E, true);

            Assert.That(GetPresenterMode(_presenter), Is.EqualTo(0));
            Assert.That(GetPresenterField(_presenter, "_container"), Is.Null);
            Assert.That(GetPresenterField(_presenter, "_containerInteractable"), Is.Null);
        }

        [Test]
        public void ChestAndCorpsePrefabs_ContainMatchingContainerAndAdapterComposition()
        {
            NetworkLootContainer chestContainer = _chestInstance.GetComponent<NetworkLootContainer>();
            NetworkLootContainerInteractable chestInteractable = _chestInstance.GetComponent<NetworkLootContainerInteractable>();
            NetworkLootContainer corpseContainer = _corpseInstance.GetComponent<NetworkLootContainer>();
            NetworkLootContainerInteractable corpseInteractable = _corpseInstance.GetComponent<NetworkLootContainerInteractable>();

            Assert.That(chestContainer, Is.Not.Null);
            Assert.That(chestInteractable, Is.Not.Null);
            Assert.That(corpseContainer, Is.Not.Null);
            Assert.That(corpseInteractable, Is.Not.Null);
            Assert.That(chestInteractable.GetType(), Is.EqualTo(corpseInteractable.GetType()));
            Assert.That(chestContainer.GetType(), Is.EqualTo(corpseContainer.GetType()));
        }

        [Test]
        public void DisableEnableAndUnbind_DoNotDuplicateSubscriptions()
        {
            SetPresenterField(_presenter, "_inputReader", _inputReader);
            SetPresenterMode(_presenter, 2);

            InvokeMethod(_presenter, "Subscribe");
            InvokeMethod(_presenter, "Subscribe"); // Idempotent check

            _presenter.Close(); // Call close to verify state reset
            Assert.That(GetPresenterMode(_presenter), Is.EqualTo(0));

            InvokeMethod(_presenter, "Unsubscribe");
            InvokeMethod(_presenter, "Unsubscribe"); // Idempotent check

            Assert.That(GetPresenterField<bool>(_presenter, "_isSubscribed"), Is.False);
        }

        [Test]
        public void Reentrancy_ClosingInCallbackPreventsNetworkInputLeaking()
        {
            SetPresenterMode(_presenter, 2);
            IDisposable suppression = _inputReader.AcquireGameplayInputSuppression();
            SetPresenterField(_presenter, "_inputSuppression", suppression);
            SetPresenterField(_presenter, "_inputReader", _inputReader);

            InvokeMethod(_presenter, "Subscribe");

            SetKey(_keyboard, Key.E, true);

            PlayerNetworkInput networkInput = _inputReader.ConsumeNetworkInput();
            Assert.That(networkInput.Buttons.IsSet(PlayerInputButton.Interact), Is.False);
        }

        [Test]
        public void PhysicalReleaseRequired_AfterClosingWhileEHeldDown()
        {
            SetPresenterMode(_presenter, 2);
            IDisposable suppression = _inputReader.AcquireGameplayInputSuppression();
            SetPresenterField(_presenter, "_inputSuppression", suppression);
            SetPresenterField(_presenter, "_inputReader", _inputReader);

            InvokeMethod(_presenter, "Subscribe");

            SetKey(_keyboard, Key.E, true); // Press E to close

            Assert.That(GetPresenterMode(_presenter), Is.EqualTo(0));

            // While E remains held down, network input MUST NOT have Interact set
            Assert.That(_inputReader.ConsumeNetworkInput().Buttons.IsSet(PlayerInputButton.Interact), Is.False);

            // Release E physically
            SetKey(_keyboard, Key.E, false);

            // Press E physically again
            SetKey(_keyboard, Key.E, true);
            InvokeReaderLifecycle(_inputReader, "Update");

            // Now Interact should be transported!
            Assert.That(_inputReader.ConsumeNetworkInput().Buttons.IsSet(PlayerInputButton.Interact), Is.True);
        }

        private static void SetKey(Keyboard keyboard, Key key, bool pressed)
        {
            using (DeltaStateEvent.From(keyboard[key], out InputEventPtr eventPtr))
            {
                eventPtr.time = InputState.currentTime;
                keyboard[key].WriteValueIntoEvent(pressed ? 1f : 0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }

            InputSystem.Update();
        }

        private static int GetPresenterMode(RaidInventoryPresenter presenter)
        {
            FieldInfo field = typeof(RaidInventoryPresenter).GetField("_mode", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return Convert.ToInt32(field.GetValue(presenter));
        }

        private static void SetPresenterMode(RaidInventoryPresenter presenter, int mode)
        {
            FieldInfo field = typeof(RaidInventoryPresenter).GetField("_mode", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            object enumVal = Enum.ToObject(field.FieldType, mode);
            field.SetValue(presenter, enumVal);
        }

        private static object GetPresenterField(RaidInventoryPresenter presenter, string fieldName)
        {
            FieldInfo field = typeof(RaidInventoryPresenter).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(presenter);
        }

        private static T GetPresenterField<T>(RaidInventoryPresenter presenter, string fieldName)
        {
            return (T)GetPresenterField(presenter, fieldName);
        }

        private static void SetPresenterField(RaidInventoryPresenter presenter, string fieldName, object value)
        {
            FieldInfo field = typeof(RaidInventoryPresenter).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(presenter, value);
        }

        private static void SetViewScreenVisible(RaidInventoryView view, bool visible)
        {
            view.SetScreenVisible(visible);
        }

        private static void SetViewContainerVisible(RaidInventoryView view, bool visible)
        {
            view.SetContainerPanelVisible(visible);
        }

        private static void InvokeMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }

        private static int ReadSuppressionCount(PlayerInputReader reader)
        {
            FieldInfo field = typeof(PlayerInputReader).GetField("_gameplaySuppressionCount", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (int)field.GetValue(reader);
        }

        private static PlayerInputActions ReadInputActions(PlayerInputReader reader)
        {
            FieldInfo actionsField = typeof(PlayerInputReader).GetField("_inputActions", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(actionsField, Is.Not.Null);
            return (PlayerInputActions)actionsField.GetValue(reader);
        }

        private static void EnsureInputEnabled(PlayerInputReader reader)
        {
            PlayerInputActions actions = ReadInputActions(reader);
            if (actions.Gameplay.enabled)
            {
                InvokeReaderLifecycle(reader, "OnDisable");
            }
            InvokeReaderLifecycle(reader, "OnEnable");
        }

        private static void InvokeReaderLifecycle(PlayerInputReader reader, string methodName)
        {
            MethodInfo method = typeof(PlayerInputReader).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(reader, null);
        }
    }
}
#endif
