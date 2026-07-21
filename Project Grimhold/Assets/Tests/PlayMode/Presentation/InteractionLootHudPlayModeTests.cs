#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Presentation
{
    public sealed class InteractionLootHudPlayModeTests
    {
        private GameObject _holder;
        private InteractionHudPresenter _presenter;
        private GameObject _promptRoot;
        private GameObject _feedbackRoot;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("InteractionHudTest");
            _presenter = _holder.AddComponent<InteractionHudPresenter>();
            _promptRoot = new GameObject(
                "Prompt",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            _feedbackRoot = new GameObject(
                "Feedback",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            TMP_Text promptText = _promptRoot.GetComponent<TextMeshProUGUI>();
            TMP_Text feedbackText = _feedbackRoot.GetComponent<TextMeshProUGUI>();

            SetField("_promptRoot", _promptRoot);
            SetField("_promptText", promptText);
            SetField("_feedbackRoot", _feedbackRoot);
            SetField("_feedbackText", feedbackText);
            SetField("_lastConsumedSequence", 5);

            _promptRoot.SetActive(false);
            _feedbackRoot.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_holder);
            Object.DestroyImmediate(_promptRoot);
            Object.DestroyImmediate(_feedbackRoot);
        }

        [UnityTest]
        public IEnumerator ConfirmedResult_IsDisplayedAtMostOncePerSequence()
        {
            InvokeResult(new InteractionPresentationEvent(
                5,
                new EntityId(1),
                default,
                10,
                false,
                false,
                InteractionFailureReason.InvalidTarget));

            Assert.That(_feedbackRoot.activeSelf, Is.False);

            InvokeResult(new InteractionPresentationEvent(
                6,
                new EntityId(1),
                default,
                10,
                false,
                false,
                InteractionFailureReason.InvalidTarget));

            Assert.That(_feedbackRoot.activeSelf, Is.True);
            yield return null;

            _feedbackRoot.SetActive(false);
            InvokeResult(new InteractionPresentationEvent(
                6,
                new EntityId(1),
                default,
                10,
                false,
                false,
                InteractionFailureReason.InvalidTarget));

            Assert.That(_feedbackRoot.activeSelf, Is.False);
        }

        [Test]
        public void Unbind_HidesTransientPresentation()
        {
            _promptRoot.SetActive(true);
            _feedbackRoot.SetActive(true);

            _presenter.Unbind();

            Assert.That(_promptRoot.activeSelf, Is.False);
            Assert.That(_feedbackRoot.activeSelf, Is.False);
        }

        private void InvokeResult(InteractionPresentationEvent presentationEvent)
        {
            MethodInfo method = typeof(InteractionHudPresenter).GetMethod(
                "OnInteractionResolved",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_presenter, new object[] { presentationEvent });
        }

        private void SetField(string fieldName, object value)
        {
            FieldInfo field = typeof(InteractionHudPresenter).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(_presenter, value);
        }
    }
}
#endif
