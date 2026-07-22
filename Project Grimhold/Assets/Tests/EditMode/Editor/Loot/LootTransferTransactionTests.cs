using System.Collections.Generic;
using NUnit.Framework;

namespace Tests.EditMode.Loot
{
    public sealed class LootTransferTransactionTests
    {
        [Test]
        public void Execute_UsesRequiredValidationAndCommitOrder()
        {
            var calls = new List<string>();
            var source = new Source(calls, LootTransferFailureReason.None);
            var destination = new Destination(calls, LootTransferFailureReason.None);
            LootTransferRequest request = ValidRequest();

            LootTransferResult result = LootTransferTransaction.Execute(source, destination, request);

            Assert.That(result.Success, Is.True);
            Assert.That(calls, Is.EqualTo(new[]
            {
                "ValidateExtraction",
                "ValidateReceive",
                "CommitExtraction",
                "CommitReceive"
            }));
        }

        [Test]
        public void Execute_SourceRejection_DoesNotValidateOrCommitDestination()
        {
            var calls = new List<string>();
            var source = new Source(calls, LootTransferFailureReason.InsufficientAmount);
            var destination = new Destination(calls, LootTransferFailureReason.None);

            LootTransferResult result = LootTransferTransaction.Execute(source, destination, ValidRequest());

            Assert.That(result.FailureReason, Is.EqualTo(LootTransferFailureReason.InsufficientAmount));
            Assert.That(calls, Is.EqualTo(new[] { "ValidateExtraction" }));
        }

        [Test]
        public void Execute_DestinationRejection_DoesNotCommitEitherEndpoint()
        {
            var calls = new List<string>();
            var source = new Source(calls, LootTransferFailureReason.None);
            var destination = new Destination(calls, LootTransferFailureReason.InventoryFull);

            LootTransferResult result = LootTransferTransaction.Execute(source, destination, ValidRequest());

            Assert.That(result.FailureReason, Is.EqualTo(LootTransferFailureReason.InventoryFull));
            Assert.That(calls, Is.EqualTo(new[] { "ValidateExtraction", "ValidateReceive" }));
        }

        private static LootTransferRequest ValidRequest() => new(
            new EntityId(1),
            new EntityId(2),
            new LootId("coin"),
            4,
            30);

        private sealed class Source : ILootExtractor
        {
            private readonly List<string> _calls;
            private readonly LootTransferFailureReason _failure;

            public Source(List<string> calls, LootTransferFailureReason failure)
            {
                _calls = calls;
                _failure = failure;
            }

            public EntityId Id => new(1);

            public LootTransferFailureReason ValidateExtraction(in LootTransferRequest request)
            {
                _calls.Add("ValidateExtraction");
                return _failure;
            }

            public void CommitExtraction(in LootTransferRequest request) => _calls.Add("CommitExtraction");
        }

        private sealed class Destination : ILootReceiver
        {
            private readonly List<string> _calls;
            private readonly LootTransferFailureReason _failure;

            public Destination(List<string> calls, LootTransferFailureReason failure)
            {
                _calls = calls;
                _failure = failure;
            }

            public EntityId Id => new(2);

            public LootTransferFailureReason ValidateReceive(in LootTransferRequest request)
            {
                _calls.Add("ValidateReceive");
                return _failure;
            }

            public void CommitReceive(in LootTransferRequest request) => _calls.Add("CommitReceive");
        }
    }
}
