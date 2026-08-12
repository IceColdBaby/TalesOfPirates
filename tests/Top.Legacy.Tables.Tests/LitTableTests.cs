using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Custom;

namespace Top.Legacy.Tables.Tests
{
    public class LitTableTests
    {
        [Test]
        public void Reads_the_token_stream()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/lit.tx"));
            var table = LitTable.Read(stream);

            Assert.That(table.Entries, Has.Count.EqualTo(9));

            var scene = table.Entries[0];
            Assert.That(scene.ObjType, Is.EqualTo(1));
            Assert.That(scene.AnimType, Is.EqualTo(4));
            Assert.That(scene.File, Is.EqualTo("02010005.lgo"));
            Assert.That(scene.SubId, Is.EqualTo(0));
            Assert.That(scene.ColorOp, Is.EqualTo(9));
            Assert.That(scene.Strings, Is.EqualTo(new[] { "cobweb.TGA", "shlight.tga", "shlight.tga" }));

            var character = table.Entries[2];
            Assert.That(character.ObjType, Is.EqualTo(0));
            Assert.That(character.File, Is.EqualTo("0000000002.lgo"));
            Assert.That(character.Mask, Is.EqualTo("0030000000.TGA"));
            Assert.That(character.Strings, Has.Length.EqualTo(4));
        }

        [Test]
        public void Finds_by_type_and_file_case_insensitively()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/lit.tx"));
            var table = LitTable.Read(stream);

            Assert.That(table.Find(1, 0, "02010005.LGO"), Is.Not.Null);
            Assert.That(table.Find(2, 0, "missing.lgo"), Is.Null);
        }

        [Test]
        public void Find_distinguishes_entries_sharing_a_file_by_obj_type()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/lit.tx"));
            var table = LitTable.Read(stream);

            var scene = table.Find(1, 0, "02010005.lgo");
            var item = table.Find(2, 0, "02010005.lgo");

            Assert.That(scene.Strings, Has.Length.EqualTo(3));
            Assert.That(item.Strings, Has.Length.EqualTo(2));
        }

        [Test]
        public void Find_ignores_sub_id()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/lit.tx"));
            var table = LitTable.Read(stream);

            Assert.That(table.Find(1, 999, "02010005.lgo"), Is.Not.Null);
        }

        [Test]
        public void Throws_on_invalid_obj_type()
        {
            var bytes = "num: 1\n0) 3\n"u8.ToArray();

            using var stream = new MemoryStream(bytes);
            Assert.Throws<TableFormatException>(() => LitTable.Read(stream));
        }
    }
}
