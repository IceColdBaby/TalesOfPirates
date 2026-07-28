using System.Collections.Generic;
using System.IO;
using Top.Text;

namespace Top.Tables.Custom
{
    public class TipsList
    {
        public IReadOnlyList<string> Lines;

        public int Count => Lines.Count;

        public static TipsList Read(Stream stream)
        {
            var lines = new List<string>(Gbk.ReadLines(stream));

            if (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return new TipsList { Lines = lines };
        }
    }
}
