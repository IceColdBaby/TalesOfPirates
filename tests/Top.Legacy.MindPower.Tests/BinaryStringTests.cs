using System.IO;
using NUnit.Framework;

namespace Top.Legacy.MindPower.Tests
{
    public class BinaryStringTests
    {
        [Test]
        public void Reads_name_up_to_nul_ignoring_padding()
        {
            var raw = new byte[] { (byte)'a', (byte)'b', 0, 0xDE, 0xAD, 0xBE, 0xEF, 0x01 };
            using var input = new MemoryStream(raw);
            Assert.That(new BinaryReader(input).ReadFixedString(8), Is.EqualTo("ab"));
        }

        [Test]
        public void Decodes_gbk_names()
        {
            var raw = new byte[] { 0xD6, 0xD0, 0xB9, 0xFA, 0, 0, 0, 0 };
            using var input = new MemoryStream(raw);
            Assert.That(new BinaryReader(input).ReadFixedString(8), Is.EqualTo("中国"));
        }

        [Test]
        public void Writes_name_nul_padded_to_width()
        {
            using var output = new MemoryStream();
            new BinaryWriter(output).WriteFixedString("ab", 8);
            Assert.That(output.ToArray(),
                Is.EqualTo(new byte[] { (byte)'a', (byte)'b', 0, 0, 0, 0, 0, 0 }));
        }

        [Test]
        public void Name_round_trips_canonically_through_nul_padding()
        {
            using var output = new MemoryStream();
            new BinaryWriter(output).WriteFixedString("中国", 16);
            using var input = new MemoryStream(output.ToArray());
            Assert.That(new BinaryReader(input).ReadFixedString(16), Is.EqualTo("中国"));
        }
    }
}
