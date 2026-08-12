using System.IO;
using NUnit.Framework;
using Top.Legacy.MindPower.Geometry;

namespace Top.Legacy.MindPower.Tests
{
    public class LgoGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // zone1.lgo: EXP_OBJ_VERSION_1_0_0_5 (0x1005), single material, 1-vertex billboard quad,
            // FVF = D3DFVF_XYZ | D3DFVF_NORMAL | D3DFVF_TEX1 (0x112), Dummy + BoundingSphere helpers.
            using var stream = File.OpenRead(Fixtures.Path("lgo/zone1.lgo"));
            var obj = LgoFile.Read(stream).Object;

            Assert.That(obj.Version, Is.EqualTo(0x1005u));
            Assert.That(obj.Id, Is.EqualTo(0u));
            Assert.That(obj.ParentId, Is.EqualTo(0xFFFFFFFFu)); // LW_INVALID_INDEX (root)

            Assert.That(obj.Materials, Is.Not.Null);
            Assert.That(obj.Materials.Length, Is.EqualTo(1));
            Assert.That(obj.Materials[0].RenderStates.Length, Is.EqualTo(8));
            Assert.That(obj.Materials[0].Stages.Length, Is.EqualTo(4));
            Assert.That(obj.Materials[0].Stages[0].FileName, Is.EqualTo("10130003.BMP"));

            Assert.That(obj.Mesh, Is.Not.Null);
            Assert.That(obj.Mesh.Fvf, Is.EqualTo(0x112u));
            Assert.That(obj.Mesh.Vertices.Length, Is.EqualTo(1));
            Assert.That(obj.Mesh.Indices.Length, Is.EqualTo(6));
            Assert.That(obj.Mesh.Subsets.Length, Is.EqualTo(1));
            Assert.That(obj.Mesh.Normals, Is.Not.Null); // D3DFVF_NORMAL set

            Assert.That(obj.Helper, Is.Not.Null);
            Assert.That(obj.Helper.Type, Is.EqualTo(HelperType.Dummy | HelperType.BoundingSphere));

            Assert.That(obj.Animation, Is.Null);
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("lgo/zone1.lgo", LgoFile.Read, (s, f) => f.Write(s));
        }

        // One fixture per distinct on-disk header version found in the corpus sweep
        // (histogram: 0x0000=91, 0x1004=234, 0x1005=6457). Smallest file of each version.

        [Test]
        public void Round_trips_v0000()
        {
            // dirk.lgo: legacy EXP_OBJ_VERSION_0_0_0_0 — exercises the v0 material (mtlVer 0) and
            // the 128-byte legacy mesh render-state set (LegacyMeshVersion 0).
            using (var stream = File.OpenRead(Fixtures.Path("lgo/dirk.lgo")))
            {
                var obj = LgoFile.Read(stream).Object;
                Assert.That(obj.Version, Is.EqualTo(0x0000u));
                Assert.That(obj.Mesh.LegacyMeshVersion, Is.EqualTo(0));
                Assert.That(obj.Helper.Type, Is.EqualTo(HelperType.BoundingBox));
            }

            GoldenAssert.RoundTrips("lgo/dirk.lgo", LgoFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        public void Round_trips_v1004()
        {
            // stone01.lgo: EXP_OBJ_VERSION_1_0_0_4 — vertex-element + 96-byte atom render-state path.
            using (var stream = File.OpenRead(Fixtures.Path("lgo/stone01.lgo")))
            {
                var obj = LgoFile.Read(stream).Object;
                Assert.That(obj.Version, Is.EqualTo(0x1004u));
                Assert.That(obj.Mesh.Vertices.Length, Is.EqualTo(17));
            }

            GoldenAssert.RoundTrips("lgo/stone01.lgo", LgoFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("lgo", "*.lgo",
                LgoFile.Read, (s, f) => f.Write(s));
        }
    }
}
