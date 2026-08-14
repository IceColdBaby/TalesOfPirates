using System;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Records;
using Top.Logging;
using Converted = Top.Contracts.Assets.Maps;
using Original = Top.Legacy.MindPower.World;

namespace Top.Conversion.Pipeline.Maps
{
    /// <summary>
    /// Builds a converted map from a .map terrain file and its .obj objects.
    /// Both tile record versions convert, in meters and degrees.
    /// </summary>
    public class MapBuilder
    {
        public const int ChunkSize = 64;

        private const int ModelKind = 0;
        private const int EffectKind = 1;
        private const int CatalogIdMask = 0x3FFF;
        private const int Centimeters = 100;
        private const float RadianHundredths = 100f;

        /// <summary>
        /// The base layer always covers fully. The earlier record's own value is
        /// dropped, as TileInfo_8To5 drops it (MPMapDef.h).
        /// </summary>
        private const byte BaseMask = 15;

        private static readonly Converted.MapTile Unwritten = new Converted.MapTile
        {
            ColorR = byte.MaxValue,
            ColorG = byte.MaxValue,
            ColorB = byte.MaxValue,
        };

        private readonly Original.MapFile _terrain;
        private readonly Original.ObjFile _objects;
        private readonly TerrainPalette _palette;

        private int _offMap;
        private int _openSea;
        private int _unknownKind;

        public MapBuilder(Original.MapFile terrain, Original.ObjFile objects, Table<TerrainInfoRecord> terrainTable)
        {
            _terrain = terrain;
            _objects = objects;
            _palette = new TerrainPalette(terrainTable);
        }

        public Converted.MapFile Build()
        {
            // Chunks first - the palette is incomplete until every tile is read.
            var countX = ChunkCount(_terrain.Width);
            var countY = ChunkCount(_terrain.Height);
            var chunks = new Converted.MapChunk[countX, countY];

            for (var chunkY = 0; chunkY < countY; chunkY++)
            {
                for (var chunkX = 0; chunkX < countX; chunkX++)
                {
                    chunks[chunkX, chunkY] = Chunk(chunkX * ChunkSize, chunkY * ChunkSize);
                }
            }

            var map = new Converted.MapFile(_terrain.Width, _terrain.Height, ChunkSize, _palette.Paths());

            Array.Copy(chunks, map.Chunks, chunks.Length);

            Place(map);

            return map;
        }

        private static int ChunkCount(int tiles)
        {
            return (tiles + ChunkSize - 1) / ChunkSize;
        }

        /// <summary>
        /// The current record packs color as B5G6R5, not R5G6B5
        /// (LW_RGB565TODWORD, MapDataVer.cpp). The earlier one stores 0xAARRGGBB.
        /// </summary>
        private static uint Color(Original.MapTile tile, bool oldFormat)
        {
            if (oldFormat)
            {
                return tile.Color888;
            }

            var packed = (ushort)tile.Color565;
            var red = (uint)((packed & 0x001F) << 3);
            var green = (uint)((packed & 0x07E0) >> 3);
            var blue = (uint)((packed & 0xF800) >> 8);

            return (red << 16) | (green << 8) | blue;
        }

        private Converted.MapChunk Chunk(int originX, int originY)
        {
            if (!Written(originX, originY))
            {
                return null;
            }

            var chunk = new Converted.MapChunk(ChunkSize);

            for (var y = 0; y < ChunkSize; y++)
            {
                for (var x = 0; x < ChunkSize; x++)
                {
                    chunk.Tiles[(y * ChunkSize) + x] = Tile(originX + x, originY + y);
                }
            }

            return chunk;
        }

        private bool Written(int originX, int originY)
        {
            var firstX = originX / _terrain.SectionWidth;
            var firstY = originY / _terrain.SectionHeight;
            var lastX = Math.Min((originX + ChunkSize - 1) / _terrain.SectionWidth, _terrain.SectionCountX - 1);
            var lastY = Math.Min((originY + ChunkSize - 1) / _terrain.SectionHeight, _terrain.SectionCountY - 1);

            for (var sectionY = firstY; sectionY <= lastY; sectionY++)
            {
                for (var sectionX = firstX; sectionX <= lastX; sectionX++)
                {
                    if (_terrain.Sections[(sectionY * _terrain.SectionCountX) + sectionX] != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private Converted.MapTile Tile(int x, int y)
        {
            var source = OriginalTile(x, y);

            if (source == null)
            {
                return Unwritten;
            }

            var oldFormat = _terrain.IsOldFormat;
            var color = Color(source, oldFormat);

            return new Converted.MapTile
            {
                Height = (oldFormat ? source.HeightCm : source.HeightStep * 10) / (float)Centimeters,
                ColorR = (byte)((color >> 16) & 0xFF),
                ColorG = (byte)((color >> 8) & 0xFF),
                ColorB = (byte)(color & 0xFF),
                Layer0 = Layer(source.Texture0, BaseMask),
                Layer1 = Layer(source.Texture1, source.Alpha1),
                Layer2 = Layer(source.Texture2, source.Alpha2),
                Layer3 = Layer(source.Texture3, source.Alpha3),
                Region = (ushort)source.Region,
                Island = source.Island,
                Corner00 = source.Block[0],
                Corner10 = source.Block[1],
                Corner01 = source.Block[2],
                Corner11 = source.Block[3],
            };
        }

        private Original.MapTile OriginalTile(int x, int y)
        {
            var sectionX = x / _terrain.SectionWidth;
            var sectionY = y / _terrain.SectionHeight;

            if (sectionX >= _terrain.SectionCountX || sectionY >= _terrain.SectionCountY)
            {
                return null;
            }

            var section = _terrain.Sections[(sectionY * _terrain.SectionCountX) + sectionX];

            return section?.Tiles[((y % _terrain.SectionHeight) * _terrain.SectionWidth)
                                  + (x % _terrain.SectionWidth)];
        }

        private Converted.MapTileLayer Layer(byte terrainId, byte mask)
        {
            var index = _palette.IndexOf(terrainId);

            return new Converted.MapTileLayer
            {
                PaletteIndex = index,
                MaskIndex = index == 0 ? (byte)0 : mask,
            };
        }

        /// <summary>
        /// Object positions are relative to their section, so the section corner
        /// is added back (CSceneObjFile::ReadSectionObjInfo, SceneObjFile.cpp).
        /// </summary>
        private void Place(Converted.MapFile map)
        {
            if (_objects?.Sections == null)
            {
                return;
            }

            for (var i = 0; i < _objects.Sections.Length; i++)
            {
                var section = _objects.Sections[i];

                if (section?.Objects == null)
                {
                    continue;
                }

                var originX = (i % _objects.SectionCountX) * _objects.SectionWidth * Centimeters;
                var originY = (i / _objects.SectionCountX) * _objects.SectionHeight * Centimeters;

                foreach (var placed in section.Objects)
                {
                    Place(map, placed, originX, originY);
                }
            }

            if (_unknownKind > 0)
            {
                Log.Warning($"dropped {_unknownKind} placements of a kind the map format has no record for");
            }

            if (_offMap > 0)
            {
                Log.Warning($"dropped {_offMap} placements standing past the map edge");
            }

            if (_openSea > 0)
            {
                Log.Warning($"dropped {_openSea} placements standing where no section wrote terrain");
            }
        }

        private void Place(Converted.MapFile map, Original.SceneObject placed, int originX, int originY)
        {
            var kind = ((ushort)placed.TypeId >> 14) & 3;

            if (kind != ModelKind && kind != EffectKind)
            {
                _unknownKind++;

                return;
            }

            var x = (placed.X + originX) / (float)Centimeters;
            var y = (placed.Y + originY) / (float)Centimeters;
            var chunkX = (int)Math.Floor(x / ChunkSize);
            var chunkY = (int)Math.Floor(y / ChunkSize);

            if (chunkX < 0 || chunkY < 0 || chunkX >= map.ChunkCountX || chunkY >= map.ChunkCountY)
            {
                _offMap++;

                return;
            }

            if (map.Chunks[chunkX, chunkY] == null)
            {
                _openSea++;

                return;
            }

            map.Chunks[chunkX, chunkY].Placements.Add(new Converted.MapPlacement
            {
                Kind = kind == ModelKind ? Converted.PlacementKind.Model : Converted.PlacementKind.Effect,
                CatalogId = placed.TypeId & CatalogIdMask,
                X = x,
                Y = y,
                HeightOffset = placed.HeightOff / (float)Centimeters,
                Yaw = kind == ModelKind
                    ? placed.YawAngle
                    : (placed.YawAngle / RadianHundredths) * (180f / MathF.PI),
            });
        }
    }
}
