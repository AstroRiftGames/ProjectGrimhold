#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.PlayMode.Presentation
{
    public sealed class RaidInventoryViewPlayModeTests
    {
        private GameObject _holder;
        private GameObject _screen;
        private GameObject _slotTemplate;
        private RectTransform _container;
        private RaidInventoryView _view;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("InventoryViewTest", typeof(RectTransform));
            _view = _holder.AddComponent<RaidInventoryView>();
            _screen = new GameObject("Screen", typeof(RectTransform));
            _container = new GameObject("Slots", typeof(RectTransform)).GetComponent<RectTransform>();
            _container.SetParent(_holder.transform);
            _slotTemplate = CreateSlotTemplate();

            SetField(_view, "_screenRoot", _screen);
            SetField(_view, "_slotContainer", _container);
            SetField(_view, "_slotPrefab", _slotTemplate.GetComponent<RaidInventorySlotView>());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_holder);
            Object.DestroyImmediate(_screen);
            Object.DestroyImmediate(_slotTemplate);
        }

        [UnityTest]
        public IEnumerator PoolIsStableForUnchangedCapacityAndClearKeepsSlots()
        {
            Assert.That(_view.EnsureSlotCount(2), Is.True);
            yield return null;
            GameObject firstSlot = _container.GetChild(0).gameObject;
            GameObject secondSlot = _container.GetChild(1).gameObject;

            Assert.That(_view.EnsureSlotCount(2), Is.True);
            var data = new List<RaidInventorySlotData>
            {
                RaidInventorySlotData.Create(new LootEntry(new LootId("coin"), 4), null, null),
                RaidInventorySlotData.Empty
            };
            Assert.That(_view.Present(data, 40), Is.True);
            _view.ClearContent();

            Assert.That(_view.SlotCount, Is.EqualTo(2));
            Assert.That(_container.GetChild(0).gameObject, Is.SameAs(firstSlot));
            Assert.That(_container.GetChild(1).gameObject, Is.SameAs(secondSlot));
        }

        private static GameObject CreateSlotTemplate()
        {
            var root = new GameObject("SlotTemplate", typeof(RectTransform));
            RaidInventorySlotView view = root.AddComponent<RaidInventorySlotView>();
            Image icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            TMP_Text name = new GameObject(
                "Name",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            TMP_Text amount = new GameObject(
                "Amount",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            icon.transform.SetParent(root.transform);
            name.transform.SetParent(root.transform);
            amount.transform.SetParent(root.transform);
            SetField(view, "_icon", icon);
            SetField(view, "_nameText", name);
            SetField(view, "_amountText", amount);
            return root;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
#endif
