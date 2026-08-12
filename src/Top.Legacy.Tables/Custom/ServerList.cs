using System.Collections.Generic;
using System.IO;
using Top.Legacy.Text;

namespace Top.Legacy.Tables.Custom
{
    public class ServerRegion
    {
        public string Name;
        public string Id;
    }

    public class ServerList
    {
        private const int MaxRegions = 20;

        public readonly List<ServerRegion> Regions = new List<ServerRegion>();

        public int RegionCount => Regions.Count;

        public static ServerList Read(Stream stream)
        {
            var table = new ServerList();

            foreach (var line in Gbk.ReadLines(stream))
            {
                if (line.Length == 0)
                {
                    break;
                }

                var fields = TableText.SplitFields(line, ',');

                table.Regions.Add(new ServerRegion
                {
                    Name = fields.Count > 0 ? fields[0] : string.Empty,
                    Id = fields.Count > 1 ? fields[1] : string.Empty,
                });

                if (table.Regions.Count >= MaxRegions)
                {
                    break;
                }
            }

            return table;
        }

        public int FindRegionIndex(string name)
        {
            for (var i = 0; i < Regions.Count; i++)
            {
                if (Regions[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
