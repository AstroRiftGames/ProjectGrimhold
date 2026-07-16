using NUnit.Framework;

namespace Tests.EditMode.Player
{
    public class PlayerJoinDataCodecTests
    {
        [Test]
        public void EncodeAndDecode_Melee_Succeeds()
        {
            var data = new PlayerJoinData(PlayerClassId.Melee);
            Assert.IsTrue(PlayerJoinDataCodec.TryEncode(data, out byte[] token));
            Assert.IsNotNull(token);
            Assert.IsTrue(PlayerJoinDataCodec.TryDecode(token, out PlayerJoinData decoded));
            Assert.AreEqual(PlayerClassId.Melee, decoded.ClassId);
        }

        [Test]
        public void EncodeAndDecode_Ranged_Succeeds()
        {
            var data = new PlayerJoinData(PlayerClassId.Ranged);
            Assert.IsTrue(PlayerJoinDataCodec.TryEncode(data, out byte[] token));
            Assert.IsNotNull(token);
            Assert.IsTrue(PlayerJoinDataCodec.TryDecode(token, out PlayerJoinData decoded));
            Assert.AreEqual(PlayerClassId.Ranged, decoded.ClassId);
        }

        [Test]
        public void TryEncode_RejectsNone()
        {
            var data = new PlayerJoinData(PlayerClassId.None);
            Assert.IsFalse(PlayerJoinDataCodec.TryEncode(data, out byte[] token));
            Assert.IsNull(token);
        }

        [Test]
        public void TryEncode_RejectsUnknownClass()
        {
            var data = new PlayerJoinData((PlayerClassId)99);
            Assert.IsFalse(PlayerJoinDataCodec.TryEncode(data, out byte[] token));
            Assert.IsNull(token);
        }

        [Test]
        public void TryDecode_RejectsNullToken()
        {
            Assert.IsFalse(PlayerJoinDataCodec.TryDecode(null, out PlayerJoinData data));
            Assert.AreEqual(PlayerClassId.None, data.ClassId);
        }

        [Test]
        public void TryDecode_RejectsEmptyToken()
        {
            Assert.IsFalse(PlayerJoinDataCodec.TryDecode(new byte[0], out PlayerJoinData data));
            Assert.AreEqual(PlayerClassId.None, data.ClassId);
        }

        [Test]
        public void TryDecode_RejectsIncorrectLength()
        {
            Assert.IsFalse(PlayerJoinDataCodec.TryDecode(new byte[] { 1 }, out PlayerJoinData data));
            Assert.IsFalse(PlayerJoinDataCodec.TryDecode(new byte[] { 1, 1, 1 }, out PlayerJoinData data2));
        }

        [Test]
        public void TryDecode_RejectsUnknownVersion()
        {
            Assert.IsFalse(PlayerJoinDataCodec.TryDecode(new byte[] { 99, (byte)PlayerClassId.Melee }, out PlayerJoinData data));
        }

        [Test]
        public void TryDecode_RejectsNone()
        {
            Assert.IsFalse(PlayerJoinDataCodec.TryDecode(new byte[] { 1, (byte)PlayerClassId.None }, out PlayerJoinData data));
        }

        [Test]
        public void TryDecode_RejectsUnknownClassValue()
        {
            Assert.IsFalse(PlayerJoinDataCodec.TryDecode(new byte[] { 1, 99 }, out PlayerJoinData data));
        }

        [Test]
        public void IsSupported_AcceptsOnlyMeleeAndRanged()
        {
            Assert.IsTrue(PlayerJoinDataCodec.IsSupported(PlayerClassId.Melee));
            Assert.IsTrue(PlayerJoinDataCodec.IsSupported(PlayerClassId.Ranged));
            Assert.IsFalse(PlayerJoinDataCodec.IsSupported(PlayerClassId.None));
            Assert.IsFalse(PlayerJoinDataCodec.IsSupported((PlayerClassId)99));
        }
    }
}
