using System.IO;
using System.Text;

namespace Top.Legacy.MindPower.World
{
    /// <summary>
    /// A .map terrain file.
    /// <br/> MPTerrainData, (TerrainData.h)
    /// </summary>
    public class MapFile
    {
        private const int OldFormatFlag = 780626;
        private const int NewFormatFlag = 780627;

        public int MapFlag;
        public int Width;
        public int Height;
        public int SectionWidth;
        public int SectionHeight;
        public MapSection[] Sections;

        public bool IsOldFormat
        {
            get
            {
                return MapFlag == OldFormatFlag;
            }
        }

        public int SectionCountX
        {
            get
            {
                return Width / SectionWidth;
            }
        }

        public int SectionCountY
        {
            get
            {
                return Height / SectionHeight;
            }
        }

        public static MapFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var mapFlag = r.ReadInt32();

            if (mapFlag != OldFormatFlag && mapFlag != NewFormatFlag)
            {
                throw new ParseException("map", (uint)mapFlag, r.BaseStream.Position,
                    $"invalid map flag {mapFlag}");
            }

            var file = new MapFile
            {
                MapFlag = mapFlag,
                Width = r.ReadInt32(),
                Height = r.ReadInt32(),
                SectionWidth = r.ReadInt32(),
                SectionHeight = r.ReadInt32(),
            };

            if (file.SectionWidth <= 0 || file.SectionHeight <= 0
                                       || file.Width < 0 || file.Height < 0)
            {
                throw new ParseException("map", (uint)mapFlag, r.BaseStream.Position,
                    $"implausible dimensions {file.Width}x{file.Height} section {file.SectionWidth}x{file.SectionHeight}");
            }

            var total = file.SectionCountX * file.SectionCountY;
            var offsets = new uint[total];
            for (var i = 0; i < total; i++)
            {
                offsets[i] = r.ReadUInt32();
            }

            file.Sections = new MapSection[total];
            for (var i = 0; i < total; i++)
            {
                if (offsets[i] == 0)
                {
                    continue;
                }

                r.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
                var tiles = new MapTile[MapSerialization.TilesPerSection];
                for (var t = 0; t < MapSerialization.TilesPerSection; t++)
                {
                    tiles[t] = file.IsOldFormat ? r.ReadOldTile() : r.ReadNewTile();
                }

                file.Sections[i] = new MapSection { Tiles = tiles };
            }

            return file;
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write(MapFlag);
            w.Write(Width);
            w.Write(Height);
            w.Write(SectionWidth);
            w.Write(SectionHeight);

            var total = Sections.Length;
            var headerBytes = 20 + total * 4;
            var offsets = new uint[total];
            var dataPosition = headerBytes;

            for (var i = 0; i < total; i++)
            {
                if (Sections[i] == null)
                {
                    continue;
                }

                offsets[i] = (uint)dataPosition;
                dataPosition += MapSerialization.TilesPerSection * (IsOldFormat ? 21 : 15);
            }

            for (var i = 0; i < total; i++)
            {
                w.Write(offsets[i]);
            }

            for (var i = 0; i < total; i++)
            {
                if (Sections[i] == null)
                {
                    continue;
                }

                foreach (var tile in Sections[i].Tiles)
                {
                    if (IsOldFormat)
                    {
                        w.WriteOldTile(tile);
                    }
                    else
                    {
                        w.WriteNewTile(tile);
                    }
                }
            }

            w.Flush();
        }
    }
}
