using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Custom;

namespace Top.Legacy.Tables.Tests
{
    public class HelpTextTableTests
    {
        [Test]
        public void Reads_help_info_whole()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/HelpInfo.tx"));
            var help = HelpTextTable.Read(stream);

            Assert.That(help.Text, Does.StartWith("_Official Build Version 0.1.1\n"));
            Assert.That(help.Text, Does.EndWith("Copyrights 2020"));
            Assert.That(help.Text.Length, Is.EqualTo(188));
        }

        [Test]
        public void Reads_store_help_whole()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/StoreHelp.tx"));
            var help = HelpTextTable.Read(stream);

            Assert.That(help.Text, Does.Contain("_Question: How to save on shopping fee?"));
        }

        [Test]
        public void Crlf_pairs_collapse_but_a_lone_cr_survives()
        {
            // "A" CRLF "B" CR "C" LF "D": the CRLF pair loses only its CR,
            // the standalone CR before "C" is not part of a pair and stays.
            var raw = "A\r\nB\rC\nD"u8.ToArray();
            using var stream = new MemoryStream(raw);

            var help = HelpTextTable.Read(stream);

            Assert.That(help.Text, Is.EqualTo("A\nB\rC\nD"));
        }

        [Test]
        public void Text_longer_than_the_buffer_is_capped_at_4095_characters()
        {
            var raw = new byte[5000];

            for (var i = 0; i < raw.Length; i++)
            {
                raw[i] = (byte)('A' + (i % 26));
            }

            using var stream = new MemoryStream(raw);

            var help = HelpTextTable.Read(stream);

            Assert.That(help.Text.Length, Is.EqualTo(4095));
            Assert.That(help.Text[4094], Is.EqualTo((char)raw[4094]));
        }
    }
}
