using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Legacy.Text;

namespace Top.Legacy.Tables.Custom
{
    public class CharacterNameFilter
    {
        public List<string> Entries = new List<string>();

        public bool IsAllowed(string text)
        {
            return Entries.All(entry => text.IndexOf(entry, StringComparison.Ordinal) < 0);
        }

        public static CharacterNameFilter Read(Stream stream)
        {
            var filter = new CharacterNameFilter();
            var lines = Gbk.ReadLines(stream);
            var count = lines.Length;

            if (count > 0 && lines[count - 1].Length == 0)
            {
                count--;
            }

            for (var i = 0; i < count; i++)
            {
                filter.Entries.Add(lines[i]);
            }

            return filter;
        }
    }
}
