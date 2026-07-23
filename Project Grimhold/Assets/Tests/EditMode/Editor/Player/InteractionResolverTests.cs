using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Player
{
    public class InteractionResolverTests
    {
        private Dictionary<EntityId, IInteractable> _mockDatabase;

        [SetUp]
        public void SetUp()
        {
            _mockDatabase = new Dictionary<EntityId, IInteractable>();
        }

        private bool Lookup(EntityId id, out IInteractable interactable)
        {
            return _mockDatabase.TryGetValue(id, out interactable);
        }

        [Test]
        public void TrySelect_SelectsWithoutExecutingInteraction()
        {
            var interactorId = new EntityId(1);
            var targetId = new EntityId(2);
            var interactable = new MockInteractable(targetId, true, true);
            _mockDatabase[targetId] = interactable;

            var candidates = new List<InteractionTarget>
            {
                new InteractionTarget(targetId, new Vector2(0.5f, 0f), 0.5f)
            };

            bool selected = InteractionResolver.TrySelect(
                interactorId,
                100,
                2f,
                candidates,
                Lookup,
                out InteractionTarget selectedTarget,
                out InteractionRequest selectedRequest,
                out IInteractable selectedInteractable);

            Assert.IsTrue(selected);
            Assert.AreEqual(targetId, selectedTarget.TargetId);
            Assert.AreEqual(targetId, selectedRequest.TargetId);
            Assert.AreSame(interactable, selectedInteractable);
            Assert.IsFalse(interactable.WasInteracted);
        }

        [Test]
        public void TryResolve_EmptyCandidatesReturnsFalse()
        {
            var interactorId = new EntityId(1);
            var candidates = new List<InteractionTarget>();

            bool result = InteractionResolver.TryResolve(
                interactorId,
                100,
                2f,
                candidates,
                Lookup,
                out var req,
                out var res
            );

            Assert.IsFalse(result);
        }

        [Test]
        public void TryResolve_ExcludesSelf()
        {
            var interactorId = new EntityId(1);
            var candidates = new List<InteractionTarget>
            {
                new InteractionTarget(interactorId, Vector2.zero, 0f)
            };

            bool result = InteractionResolver.TryResolve(
                interactorId,
                100,
                2f,
                candidates,
                Lookup,
                out _,
                out _
            );

            Assert.IsFalse(result);
        }

        [Test]
        public void TryResolve_ExcludesOutOfRangeDefensively()
        {
            var interactorId = new EntityId(1);
            var targetId = new EntityId(2);
            var interactable = new MockInteractable(targetId, true, true);
            _mockDatabase[targetId] = interactable;

            var candidates = new List<InteractionTarget>
            {
                new InteractionTarget(targetId, Vector2.zero, 2.5f) // > max distance 2f
            };

            bool result = InteractionResolver.TryResolve(
                interactorId,
                100,
                2f,
                candidates,
                Lookup,
                out _,
                out _
            );

            Assert.IsFalse(result);
        }

        [Test]
        public void TryResolve_CanInteractFalseSkipToNextCandidate()
        {
            var interactorId = new EntityId(1);
            var target1 = new EntityId(2);
            var target2 = new EntityId(3);

            var interactable1 = new MockInteractable(target1, false, true); // CanInteract = false
            var interactable2 = new MockInteractable(target2, true, true);  // CanInteract = true

            _mockDatabase[target1] = interactable1;
            _mockDatabase[target2] = interactable2;

            var candidates = new List<InteractionTarget>
            {
                new InteractionTarget(target1, Vector2.zero, 1f),
                new InteractionTarget(target2, Vector2.zero, 1.2f)
            };

            bool result = InteractionResolver.TryResolve(
                interactorId,
                100,
                2f,
                candidates,
                Lookup,
                out var req,
                out var res
            );

            Assert.IsTrue(result);
            Assert.AreEqual(target2, req.TargetId);
            Assert.IsTrue(res.Success);
            Assert.IsFalse(interactable1.WasInteracted);
            Assert.IsTrue(interactable2.WasInteracted);
        }

        [Test]
        public void TryResolve_ExecutesOnlyFirstValidTargetEvenIfInteractFails()
        {
            var interactorId = new EntityId(1);
            var target1 = new EntityId(2);
            var target2 = new EntityId(3);

            var interactable1 = new MockInteractable(target1, true, false); // CanInteract = true, Success = false
            var interactable2 = new MockInteractable(target2, true, true);  // CanInteract = true, Success = true

            _mockDatabase[target1] = interactable1;
            _mockDatabase[target2] = interactable2;

            var candidates = new List<InteractionTarget>
            {
                new InteractionTarget(target1, Vector2.zero, 1f),
                new InteractionTarget(target2, Vector2.zero, 1.2f)
            };

            bool result = InteractionResolver.TryResolve(
                interactorId,
                100,
                2f,
                candidates,
                Lookup,
                out var req,
                out var res
            );

            Assert.IsTrue(result);
            Assert.AreEqual(target1, req.TargetId);
            Assert.IsFalse(res.Success);
            Assert.IsTrue(interactable1.WasInteracted);
            Assert.IsFalse(interactable2.WasInteracted); // Should not process the second target!
        }

        [Test]
        public void TryResolve_ThrowsOnInvalidParameters()
        {
            var interactorId = new EntityId(1);
            Assert.Throws<ArgumentNullException>(() =>
                InteractionResolver.TryResolve(interactorId, 100, 2f, null, Lookup, out _, out _)
            );

            var candidates = new List<InteractionTarget>();
            Assert.Throws<ArgumentNullException>(() =>
                InteractionResolver.TryResolve(interactorId, 100, 2f, candidates, null, out _, out _)
            );
        }

        private sealed class MockInteractable : IInteractable
        {
            public EntityId ID { get; }
            private readonly bool _canInteract;
            private readonly bool _interactSuccess;

            public bool WasInteracted { get; private set; }

            public MockInteractable(EntityId id, bool canInteract, bool interactSuccess)
            {
                ID = id;
                _canInteract = canInteract;
                _interactSuccess = interactSuccess;
            }

            public bool CanInteract(in InteractionRequest request) => _canInteract;

            public InteractionResult Interact(in InteractionRequest request)
            {
                WasInteracted = true;
                return _interactSuccess ? InteractionResult.Succeeded(true) : InteractionResult.Rejected(InteractionFailureReason.InvalidTarget);
            }
        }
    }
}
