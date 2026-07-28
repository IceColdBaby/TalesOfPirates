using System.IO;
using System.Numerics;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class AreaSetTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/areaset.txt"));
            var table = TableFile.Read<AreaRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Argent City"));
            Assert.That(record.Color, Is.EqualTo(new[] { 41, 107, 214 }));
            Assert.That(record.Music, Is.EqualTo(2));
            Assert.That(record.EnvColor, Is.EqualTo(new[] { 114, 148, 155 }));
            Assert.That(record.LightColor, Is.EqualTo(new[] { 255, 255, 255 }));
            Assert.That(record.LightDir, Is.EqualTo(new Vector3(-1f, -1f, -1f)));
            Assert.That(record.IsCity, Is.True);
        }
    }
}
