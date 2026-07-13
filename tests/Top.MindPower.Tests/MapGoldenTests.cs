using System.IO;
using NUnit.Framework;
using Top.MindPower.World;

namespace Top.MindPower.Tests
{
    public class MapGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // room.map: new layout (flag 780627 = CUR_VERSION_NO, MPMapDef.h:14). 52x52 tiles,
            // 8x8 sections => 6x6 = 36 sections, all present (SectionCountX = Width/SectionWidth,
            // TerrainData.h:57). First section's tile[0] carries texture 46, height-step 5, region 1,
            // island 0, block bytes 10/10/10/10 (SNewFileTile, MPMapDef.h:30).
            using var stream = File.OpenRead(Fixtures.Path("map/room.map"));
            var file = MapFile.Read(stream);

            Assert.That(file.MapFlag, Is.EqualTo(780627));
            Assert.That(file.IsOldFormat, Is.False);
            Assert.That(file.Width, Is.EqualTo(52));
            Assert.That(file.Height, Is.EqualTo(52));
            Assert.That(file.SectionWidth, Is.EqualTo(8));
            Assert.That(file.SectionHeight, Is.EqualTo(8));
            Assert.That(file.SectionCountX, Is.EqualTo(6));
            Assert.That(file.SectionCountY, Is.EqualTo(6));
            Assert.That(file.Sections.Length, Is.EqualTo(36));

            var section0 = file.Sections[0];
            Assert.That(section0, Is.Not.Null);
            Assert.That(section0.Tiles.Length, Is.EqualTo(64));

            var tile0 = section0.Tiles[0];
            Assert.That(tile0.Texture0, Is.EqualTo((byte)46));
            Assert.That(tile0.Alpha0, Is.EqualTo((byte)15));
            Assert.That(tile0.Texture1, Is.EqualTo((byte)0));
            Assert.That(tile0.Alpha1, Is.EqualTo((byte)0));
            Assert.That(tile0.Texture2, Is.EqualTo((byte)0));
            Assert.That(tile0.Alpha2, Is.EqualTo((byte)0));
            Assert.That(tile0.Texture3, Is.EqualTo((byte)0));
            Assert.That(tile0.Alpha3, Is.EqualTo((byte)0));
            Assert.That(tile0.HeightStep, Is.EqualTo((sbyte)5));
            Assert.That(tile0.Region, Is.EqualTo((short)1));
            Assert.That(tile0.Island, Is.EqualTo((byte)0));
            Assert.That(tile0.Block, Is.EqualTo(new byte[] { 10, 10, 10, 10 }));
        }

        [Test]
        public void New_tile_unpacks_each_packed_layer()
        {
            // room.map carries dwTileInfo == 0 for every tile, so the fixture cannot
            // exercise the upper layers. Drive a synthetic dwTileInfo with a distinct
            // value per field through Write/Read to pin TileInfo_8To5/TileInfo_5To8
            // (MPMapDef.h:74/88): tex 6 bits, alpha 4 bits, bits 1..0 unused.
            const uint dwTileInfo =
                ((uint)61 << 26) | ((uint)14 << 22)
                                 | ((uint)37 << 16) | ((uint)9 << 12)
                                 | ((uint)18 << 6) | ((uint)3 << 2);

            var file = new MapFile
            {
                MapFlag = 780627,
                Width = 8,
                Height = 8,
                SectionWidth = 8,
                SectionHeight = 8,
            };

            var source = new MapTile
            {
                Texture0 = 46,
                Alpha0 = 15,
                Texture1 = (byte)((dwTileInfo >> 26) & 0x3F),
                Alpha1 = (byte)((dwTileInfo >> 22) & 0x0F),
                Texture2 = (byte)((dwTileInfo >> 16) & 0x3F),
                Alpha2 = (byte)((dwTileInfo >> 12) & 0x0F),
                Texture3 = (byte)((dwTileInfo >> 6) & 0x3F),
                Alpha3 = (byte)((dwTileInfo >> 2) & 0x0F),
                Color565 = 0,
                HeightStep = 0,
                Region = 0,
                Island = 0,
                Block = new byte[] { 0, 0, 0, 0 },
            };

            var tiles = new MapTile[64];
            for (var i = 0; i < tiles.Length; i++)
            {
                tiles[i] = source;
            }

            file.Sections = new[] { new MapSection { Tiles = tiles } };

            using var mem = new MemoryStream();
            file.Write(mem);
            mem.Position = 0;
            var tile = MapFile.Read(mem).Sections[0].Tiles[0];

            Assert.That(tile.Texture0, Is.EqualTo((byte)46));
            Assert.That(tile.Alpha0, Is.EqualTo((byte)15));
            Assert.That(tile.Texture1, Is.EqualTo((byte)61));
            Assert.That(tile.Alpha1, Is.EqualTo((byte)14));
            Assert.That(tile.Texture2, Is.EqualTo((byte)37));
            Assert.That(tile.Alpha2, Is.EqualTo((byte)9));
            Assert.That(tile.Texture3, Is.EqualTo((byte)18));
            Assert.That(tile.Alpha3, Is.EqualTo((byte)3));
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("map/room.map", MapFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("map", "*.map",
                MapFile.Read, (s, f) => f.Write(s));
        }
    }
}
