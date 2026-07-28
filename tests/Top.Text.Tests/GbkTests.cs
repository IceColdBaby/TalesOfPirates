using System.IO;
using NUnit.Framework;

namespace Top.Text.Tests
{
    public class GbkTests
    {
        [Test]
        public void DecodesAscii()
        {
            Assert.AreEqual("AB", Gbk.GetString(new byte[] { 0x41, 0x42 }));
        }

        [Test]
        public void DecodesGb2312()
        {
            Assert.AreEqual("中国", Gbk.GetString(new byte[] { 0xD6, 0xD0, 0xB9, 0xFA })); // 中国
        }

        [Test]
        public void DecodesFullwidthPunctuation()
        {
            Assert.AreEqual("：", Gbk.GetString(new byte[] { 0xA3, 0xBA }));
        }

        [Test]
        public void DecodesGbkExtensionCodepoint()
        {
            Assert.AreEqual("丂", Gbk.GetString(new byte[] { 0x81, 0x40 }));
        }

        [Test]
        public void DecodesMixedAsciiAndChinese()
        {
            Assert.AreEqual("id=中", Gbk.GetString(new byte[] { 0x69, 0x64, 0x3D, 0xD6, 0xD0 }));
        }

        [Test]
        public void ReturnsEmptyForNullOrEmpty()
        {
            Assert.AreEqual(string.Empty, Gbk.GetString(null));
            Assert.AreEqual(string.Empty, Gbk.GetString(new byte[0]));
        }

        [Test]
        public void EncodesAscii()
        {
            Assert.AreEqual(new byte[] { 0x41, 0x42 }, Gbk.GetBytes("AB"));
        }

        [Test]
        public void EncodesGb2312RoundTrip()
        {
            var bytes = Gbk.GetBytes("中国");
            Assert.AreEqual(new byte[] { 0xD6, 0xD0, 0xB9, 0xFA }, bytes);
            Assert.AreEqual("中国", Gbk.GetString(bytes));
        }

        [Test]
        public void EncodesEmptyAsEmpty()
        {
            Assert.AreEqual(new byte[0], Gbk.GetBytes(null));
            Assert.AreEqual(new byte[0], Gbk.GetBytes(""));
        }

        [Test]
        public void ReadLines_decodes_gbk_and_splits_crlf()
        {
            // GBK bytes for the two hanzi of "zhong wen" (0xD6D0 0xCEC4) between ASCII lines.
            var bytes = new byte[]
            {
                (byte)'a', (byte)'\r', (byte)'\n',
                0xD6, 0xD0, 0xCE, 0xC4, (byte)'\n',
                (byte)'b',
            };
            using var stream = new MemoryStream(bytes);

            var lines = Gbk.ReadLines(stream);

            Assert.That(lines, Is.EqualTo(new[] { "a", "中文", "b" }));
        }
    }
}
