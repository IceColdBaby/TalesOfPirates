using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Custom;

namespace Top.Legacy.Tables.Tests
{
    public class ServerListTests
    {
        [Test]
        public void Stops_at_the_first_blank_line()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/server.tx"));
            var list = ServerList.Read(stream);

            Assert.That(list.RegionCount, Is.EqualTo(1));
            Assert.That(list.Regions[0].Name, Is.EqualTo("Development"));
            Assert.That(list.Regions[0].Id, Is.EqualTo(""));
        }

        [Test]
        public void Blank_line_is_a_hard_stop_not_a_skip()
        {
            var bytes = "A\n\nB\n"u8.ToArray();
            using var stream = new MemoryStream(bytes);
            var list = ServerList.Read(stream);

            Assert.That(list.RegionCount, Is.EqualTo(1));
            Assert.That(list.Regions[0].Name, Is.EqualTo("A"));
            Assert.That(list.FindRegionIndex("B"), Is.EqualTo(-1));
        }

        [Test]
        public void Splits_name_and_id_on_comma()
        {
            var bytes = "Europe,42\n"u8.ToArray();
            using var stream = new MemoryStream(bytes);
            var list = ServerList.Read(stream);

            Assert.That(list.Regions[0].Name, Is.EqualTo("Europe"));
            Assert.That(list.Regions[0].Id, Is.EqualTo("42"));
        }
    }
}
