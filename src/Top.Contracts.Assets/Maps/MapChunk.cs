using System.Collections.Generic;

namespace Top.Contracts.Assets.Maps
{
    /// <summary>
    /// Represents a chunk of a map, containing tiles and placements.
    /// </summary>
    public class MapChunk
    {
        public readonly MapTile[] Tiles;
        public readonly List<MapPlacement> Placements = new List<MapPlacement>();

        public MapChunk(int chunkSize)
        {
            Tiles = new MapTile[chunkSize * chunkSize];
        }
    }
}
