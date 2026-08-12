using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class SceneObjectInfoTableTests
    {
        private static Table<SceneObjectInfoRecord> ReadText(string text)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
            return TableFile.Read<SceneObjectInfoRecord>(stream);
        }

        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/sceneobjinfo.txt"));
            var table = TableFile.Read<SceneObjectInfoRecord>(stream);

            var record = table[12];

            Assert.That(record.Name, Is.EqualTo("nml-bd150.lmo"));
            Assert.That(record.DisplayName, Is.EqualTo("Broken Bridge 0"));
            Assert.That(record.Type, Is.EqualTo(0));
            Assert.That(record.FadeObjSeq, Is.Empty);
            Assert.That(record.EnableEnvLight, Is.True);
            Assert.That(record.EnablePointLight, Is.False);
            Assert.That(record.Style, Is.EqualTo(9));
            Assert.That(record.SizeFlag, Is.EqualTo(1));
            Assert.That(record.ShadeFlag, Is.True);
        }

        [Test]
        public void Looks_up_model_by_name()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/sceneobjinfo.txt"));
            var table = TableFile.Read<SceneObjectInfoRecord>(stream);

            Assert.That(table.TryGetByName("nml-bd150.lmo", out var record), Is.True);
            Assert.That(record.Id, Is.EqualTo(12));
        }

        [Test]
        public void Reads_type_3_point_light_row()
        {
            var table = ReadText("101\tlightpt.lmo\tPoint Light\t3\t10,20,30\t5,1.5,7\t100\t0\t1\t42\t0\t0\t1\n");

            var record = table[101];

            Assert.That(record.PointColor, Is.EqualTo(new[] { 10, 20, 30 }));
            Assert.That(record.PointLightRange, Is.EqualTo(5));
            Assert.That(record.PointLightAttenuation, Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(record.PointLightAnimCtrlId, Is.EqualTo(7));
            Assert.That(record.Style, Is.EqualTo(42));
            Assert.That(record.ShadeFlag, Is.True);
        }

        [Test]
        public void Reads_type_4_ambient_light_row()
        {
            var table = ReadText("102\tlightamb.lmo\tAmbient Light\t4\t40,50,60\t999\t200\t1\t0\t17\t3\t2\t0\n");

            var record = table[102];

            Assert.That(record.EnvColor, Is.EqualTo(new[] { 40, 50, 60 }));
            Assert.That(record.Style, Is.EqualTo(17));
            Assert.That(record.ShadeFlag, Is.False);
        }

        [Test]
        public void Reads_type_6_environment_sound_row()
        {
            var table = ReadText("103\tsndenv.lmo\tEnv Sound\t6\twave01\t250\t310\t0\t0\t88\t5\t1\t1\n");

            var record = table[103];

            Assert.That(record.EnvSound, Is.EqualTo("wave01"));
            Assert.That(record.EnvSoundDistance, Is.EqualTo(250));
            Assert.That(record.Style, Is.EqualTo(88));
            Assert.That(record.ShadeFlag, Is.True);
        }

        [Test]
        public void Reads_type_0_fade_row()
        {
            var table = ReadText("105\tfade001.lmo\tFade Object\t0\t4,11,12,13,14,7.5\t0\t300\t1\t0\t66\t2\t3\t1\n");

            var record = table[105];

            Assert.That(record.FadeObjSeq, Is.EqualTo(new[] { 11, 12, 13, 14 }));
            Assert.That(record.FadeCoefficient, Is.EqualTo(7.5f).Within(1e-5f));
            Assert.That(record.Style, Is.EqualTo(66));
            Assert.That(record.ShadeFlag, Is.True);
        }

        [Test]
        public void Reads_unhandled_type_row()
        {
            var table = ReadText("104\tother.lmo\tOther Type\t1\tignored1\tignored2\t400\t1\t1\t55\t9\t4\t0\n");

            var record = table[104];

            Assert.That(record.PointColor, Is.Empty);
            Assert.That(record.EnvColor, Is.Empty);
            Assert.That(record.EnvSound, Is.Empty);
            Assert.That(record.Style, Is.EqualTo(55));
            Assert.That(record.ShadeFlag, Is.False);
        }
    }
}
