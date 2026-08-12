using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ShadeTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/shadeinfo.txt"));
            var table = TableFile.Read<ShadeInfoRecord>(stream);

            var record = table[5];

            Assert.That(record.Name, Is.EqualTo("1.tga"));
            Assert.That(record.DisplayName, Is.EqualTo("Black Shadow"));
            Assert.That(record.Size, Is.EqualTo(1f));
            Assert.That(record.Animated, Is.False);
            Assert.That(record.Color, Is.EqualTo(new[] { 255, 255, 255, 200 }));
            Assert.That(record.Type, Is.EqualTo(2));
        }
    }
}
