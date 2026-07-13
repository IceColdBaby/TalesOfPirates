using System.IO;
using NUnit.Framework;
using Top.MindPower.World;

namespace Top.MindPower.Tests
{
    public class ObjGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // teampk.obj: version 600 (OBJ_FILE_VER600, SceneObjFile.h:10), 12x12 sections of 8x8 tiles,
            // 25-object sections (CSceneObjData, ObjectData.h). The first populated section is index 3
            // (sx=3, sy=0) with a single scene object (type 0, id 268) at section-local (780, 490)
            // centimetres (SSceneObjInfo, SceneObjFile.h:15).
            using var stream = File.OpenRead(Fixtures.Path("obj/teampk.obj"));
            var file = ObjFile.Read(stream);

            Assert.That(file.Version, Is.EqualTo(600));
            Assert.That(file.SectionCountX, Is.EqualTo(12));
            Assert.That(file.SectionCountY, Is.EqualTo(12));
            Assert.That(file.SectionWidth, Is.EqualTo(8));
            Assert.That(file.SectionHeight, Is.EqualTo(8));
            Assert.That(file.SectionObjectCount, Is.EqualTo(25));
            Assert.That(file.Sections.Length, Is.EqualTo(144));

            // Sections 0..2 are absent (index offset 0); section 3 holds the first object.
            Assert.That(file.Sections[0], Is.Null);
            var section3 = file.Sections[3];
            Assert.That(section3, Is.Not.Null);
            Assert.That(section3.Objects.Length, Is.EqualTo(1));

            var obj0 = section3.Objects[0];
            Assert.That(obj0.TypeId, Is.EqualTo((short)268));
            Assert.That(obj0.X, Is.EqualTo(780));
            Assert.That(obj0.Y, Is.EqualTo(490));
            Assert.That(obj0.HeightOff, Is.EqualTo((short)0));
            Assert.That(obj0.YawAngle, Is.EqualTo((short)0));

            // top 2 bits = type, low 14 bits = id (SSceneObjInfo::GetType/GetID, SceneObjFile.h:24).
            Assert.That((obj0.TypeId >> 14) & 3, Is.EqualTo(0));
            Assert.That(obj0.TypeId & 0x3FFF, Is.EqualTo(268));
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("obj/teampk.obj", ObjFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("obj", "*.obj",
                ObjFile.Read, (s, f) => f.Write(s));
        }
    }
}
