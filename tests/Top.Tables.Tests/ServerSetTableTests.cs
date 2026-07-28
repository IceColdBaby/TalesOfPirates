using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class ServerSetTableTests
    {
        [Test]
        public void Trims_gate_ips_at_the_sentinel()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/serverset.txt"));
            var table = TableFile.Read<ServerRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Local"));
            Assert.That(record.Region, Is.EqualTo("Development"));
            Assert.That(record.GateIps, Is.EqualTo(new[] { "127.0.0.1" }));
        }
    }
}
