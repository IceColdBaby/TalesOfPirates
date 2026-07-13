using System.IO;
using NUnit.Framework;
using Top.MindPower.World;

namespace Top.MindPower.Tests
{
    public class BlkGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // teampk.blk: two int32 dimensions 192x192 (CBlockData::Load, util2.h:287), then a
            // (192/8)*192 = 24*192 bit-packed grid, MSB-first within each byte (_IsBlock,
            // util2.h:331). Payload byte[6*24+9] == 0x1c (00011100) => at row 6, x=75,76,77 are
            // blocked and x=72..74,78,79 are clear -- hand-decoded from the fixture bytes.
            using var stream = File.OpenRead(Fixtures.Path("blk/teampk.blk"));
            var file = BlkFile.Read(stream);

            Assert.That(file.Grid.Width, Is.EqualTo(192));
            Assert.That(file.Grid.Height, Is.EqualTo(192));
            Assert.That(file.Grid.ByteWidth, Is.EqualTo(24));
            Assert.That(file.Grid.PackedRows.Length, Is.EqualTo(24 * 192));

            // (0,0) is clear; the first non-zero byte sits at row 6, byte-col 9.
            Assert.That(file.Grid.IsBlocked(0, 0), Is.False);
            Assert.That(file.Grid.IsBlocked(72, 6), Is.False);
            Assert.That(file.Grid.IsBlocked(74, 6), Is.False);
            Assert.That(file.Grid.IsBlocked(75, 6), Is.True);
            Assert.That(file.Grid.IsBlocked(76, 6), Is.True);
            Assert.That(file.Grid.IsBlocked(77, 6), Is.True);
            Assert.That(file.Grid.IsBlocked(78, 6), Is.False);
        }

        [Test]
        public void Bit_pattern_packs_msb_first()
        {
            // Synthetic 8x1 grid pins the MSB-first bit order (_IsBlock: nBit = 7 - x%8,
            // util2.h:335). Block x=0 (top bit, 0x80) and x=3 (0x10) only.
            var file = new BlkFile
            {
                Grid = new BlockGrid(8, 1, new byte[1]),
            };
            file.Grid.SetBlocked(0, 0, true);
            file.Grid.SetBlocked(3, 0, true);

            Assert.That(file.Grid.PackedRows[0], Is.EqualTo((byte)0x90));

            using var mem = new MemoryStream();
            file.Write(mem);
            mem.Position = 0;
            var read = BlkFile.Read(mem);

            Assert.That(read.Grid.IsBlocked(0, 0), Is.True);
            Assert.That(read.Grid.IsBlocked(1, 0), Is.False);
            Assert.That(read.Grid.IsBlocked(2, 0), Is.False);
            Assert.That(read.Grid.IsBlocked(3, 0), Is.True);
            Assert.That(read.Grid.IsBlocked(7, 0), Is.False);
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("blk/teampk.blk", BlkFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("blk", "*.blk",
                BlkFile.Read, (s, f) => f.Write(s));
        }
    }
}
