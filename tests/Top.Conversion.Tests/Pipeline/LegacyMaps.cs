using System;
using Top.Legacy.MindPower.World;

namespace Top.Conversion.Tests.Pipeline
{
    internal static class LegacyMaps
    {
        internal const int NewFormat = 780627;
        internal const int OldFormat = 780626;
        internal const int SectionSize = 8;

        internal static MapFile Terrain(int flag, int width, int height)
        {
            var file = new MapFile
            {
                MapFlag = flag,
                Width = width,
                Height = height,
                SectionWidth = SectionSize,
                SectionHeight = SectionSize,
            };

            file.Sections = new MapSection[file.SectionCountX * file.SectionCountY];

            return file;
        }

        internal static MapFile FilledTerrain(int flag, int width, int height)
        {
            var file = Terrain(flag, width, height);

            for (var i = 0; i < file.Sections.Length; i++)
            {
                file.Sections[i] = Section();
            }

            return file;
        }

        internal static MapSection Section(Func<int, MapTile> tileAt)
        {
            var tiles = new MapTile[SectionSize * SectionSize];

            for (var i = 0; i < tiles.Length; i++)
            {
                tiles[i] = tileAt(i);
            }

            return new MapSection { Tiles = tiles };
        }

        internal static MapSection Section()
        {
            return Section(_ => Tile());
        }

        internal static MapTile Tile()
        {
            return new MapTile { Alpha0 = 15, Block = new byte[4] };
        }

        internal static ObjFile Objects(int width, int height)
        {
            var file = new ObjFile
            {
                Version = 600,
                SectionCountX = width / SectionSize,
                SectionCountY = height / SectionSize,
                SectionWidth = SectionSize,
                SectionHeight = SectionSize,
                SectionObjectCount = 25,
            };

            file.Sections = new ObjSection[file.SectionCountX * file.SectionCountY];

            return file;
        }

        internal static SceneObject Model(int catalogId, int x, int y, short heightOff, short yaw)
        {
            return Placed(0, catalogId, x, y, heightOff, yaw);
        }

        internal static SceneObject Effect(int catalogId, int x, int y, short heightOff, short yaw)
        {
            return Placed(1, catalogId, x, y, heightOff, yaw);
        }

        private static SceneObject Placed(int kind, int catalogId, int x, int y, short heightOff, short yaw)
        {
            return new SceneObject
            {
                TypeId = (short)((kind << 14) | catalogId),
                X = x,
                Y = y,
                HeightOff = heightOff,
                YawAngle = yaw,
            };
        }
    }
}
