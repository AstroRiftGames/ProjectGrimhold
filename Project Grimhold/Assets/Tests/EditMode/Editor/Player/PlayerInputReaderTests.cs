using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Player
{
    public sealed class PlayerInputReaderTests
    {
        private GameObject _holder;
        private PlayerInputReader _reader;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("PlayerInputReaderHolder");
            _reader = _holder.AddComponent<PlayerInputReader>();
            InvokeLifecycle("Awake");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_holder);
        }

        [Test]
        public void Suppression_ProducesDefaultPayload()
        {
            using IDisposable suppression = _reader.AcquireGameplayInputSuppression();

            Assert.That(_reader.ConsumeNetworkInput().Equals(default(PlayerNetworkInput)), Is.True);
        }

        [Test]
        public void NestedSuppressions_RequireEveryOwnerToRelease()
        {
            IDisposable first = _reader.AcquireGameplayInputSuppression();
            IDisposable second = _reader.AcquireGameplayInputSuppression();

            first.Dispose();
            Assert.That(ReadSuppressionCount(), Is.EqualTo(1));

            second.Dispose();
            Assert.That(ReadSuppressionCount(), Is.Zero);
        }

        [Test]
        public void DuplicateRelease_IsInnocuous()
        {
            IDisposable suppression = _reader.AcquireGameplayInputSuppression();

            suppression.Dispose();
            suppression.Dispose();

            Assert.That(ReadSuppressionCount(), Is.Zero);
        }

        private int ReadSuppressionCount()
        {
            FieldInfo field = typeof(PlayerInputReader).GetField(
                "_gameplaySuppressionCount",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (int)field.GetValue(_reader);
        }

        private void InvokeLifecycle(string methodName)
        {
            MethodInfo method = typeof(PlayerInputReader).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(_reader, null);
        }
    }
}
