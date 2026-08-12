using System.IO;
using NUnit.Framework;
using Top.Legacy.MindPower.Minimap;

namespace Top.Legacy.MindPower.Tests
{
    public class PkGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // hell4.pk: root path "texture\minimap\hell4", 9 tile BMPs (sm_0_0..sm_2_2),
            // no subdirectories (CPackFile, PackFile.cpp:75). First entry sm_0_0.bmp is 4644 bytes;
            // every payload is a Windows BMP, so it begins with the "BM" magic (0x42 0x4D).
            using var stream = File.OpenRead(Fixtures.Path("pk/hell4.pk"));
            var file = PkFile.Read(stream);

            Assert.That(file.Name, Is.EqualTo("texture\\minimap\\hell4"));
            Assert.That(file.Files.Length, Is.EqualTo(9));
            Assert.That(file.Directories.Length, Is.EqualTo(0));

            Assert.That(file.Files[0].Name, Is.EqualTo("sm_0_0.bmp"));
            Assert.That(file.Files[0].Payload.Length, Is.EqualTo(4644));

            foreach (var entry in file.Files)
            {
                Assert.That(entry.Payload.Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(entry.Payload[0], Is.EqualTo((byte)0x42), $"{entry.Name} missing BM magic");
                Assert.That(entry.Payload[1], Is.EqualTo((byte)0x4D), $"{entry.Name} missing BM magic");
            }
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("pk/hell4.pk", PkFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        public void Names_round_trip_untouched()
        {
            // The lobby.pk quirk: names may embed a foreign path; the reader must not normalize them.
            // Drive an embedded path plus a leading/trailing space through Write/Read to pin verbatim names.
            var file = new PkFile
            {
                Name = "texture\\minimap\\garner",
                Directories = new PkDirectory[0],
                Files = new[]
                {
                    new PkEntry { Name = " sm_0_0.bmp ", Payload = new byte[] { 0x42, 0x4D, 1, 2 } },
                    new PkEntry
                    {
                        Name = "texture\\minimap\\garner\\sm_1_1.bmp", Payload = new byte[] { 0x42, 0x4D, 3 }
                    },
                },
            };

            using var mem = new MemoryStream();
            file.Write(mem);
            mem.Position = 0;
            var read = PkFile.Read(mem);

            Assert.That(read.Name, Is.EqualTo("texture\\minimap\\garner"));
            Assert.That(read.Files[0].Name, Is.EqualTo(" sm_0_0.bmp "));
            Assert.That(read.Files[1].Name, Is.EqualTo("texture\\minimap\\garner\\sm_1_1.bmp"));
            Assert.That(read.Files[0].Payload, Is.EqualTo(new byte[] { 0x42, 0x4D, 1, 2 }));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("pk", "*.pk",
                PkFile.Read, (s, f) => f.Write(s));
        }
    }
}
