using System.Collections.Generic;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Records;
using Top.Logging;

namespace Top.Conversion.Pipeline.Maps
{
    /// <summary>
    /// One map's texture palette, resolving terraininfo ids to converted tree
    /// paths. Index 0 means no texture, as terrain id 0 did.
    /// </summary>
    public class TerrainPalette
    {
        private const string TextureKind = "terrain";
        private const int MaxIndex = byte.MaxValue;

        private readonly Table<TerrainInfoRecord> _terrain;
        private readonly Dictionary<byte, byte> _indices = new Dictionary<byte, byte>();
        private readonly List<string> _paths = new List<string> { string.Empty };
        private readonly HashSet<byte> _reported = new HashSet<byte>();

        public TerrainPalette(Table<TerrainInfoRecord> terrain)
        {
            _terrain = terrain;
        }

        public string[] Paths()
        {
            return _paths.ToArray();
        }

        public byte IndexOf(byte terrainId)
        {
            if (terrainId == 0)
            {
                return 0;
            }

            if (_indices.TryGetValue(terrainId, out var index))
            {
                return index;
            }

            if (_terrain == null || !_terrain.TryGetById(terrainId, out var row) || string.IsNullOrEmpty(row.Name))
            {
                Report(terrainId, $"no terraininfo row {terrainId}, leaving the layer unpainted");

                return 0;
            }

            if (_paths.Count > MaxIndex)
            {
                Report(terrainId,
                    $"more than {MaxIndex} textures on one map, leaving terrain {terrainId} unpainted");

                return 0;
            }

            index = (byte)_paths.Count;

            _paths.Add(OutputPaths.TextureTreePath(TextureKind, row.Name));
            _indices.Add(terrainId, index);

            return index;
        }

        private void Report(byte terrainId, string message)
        {
            if (_reported.Add(terrainId))
            {
                Log.Warning(message);
            }
        }
    }
}
