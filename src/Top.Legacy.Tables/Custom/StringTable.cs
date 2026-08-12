using System.Collections.Generic;
using System.IO;
using Top.Legacy.Text;

namespace Top.Legacy.Tables.Custom
{
    public class StringTable
    {
        private readonly Dictionary<int, string> _strings = new Dictionary<int, string>();

        public int Count => _strings.Count;

        public string GetString(int id)
        {
            return _strings.TryGetValue(id, out var value) ? value : string.Empty;
        }

        public static StringTable Read(Stream stream)
        {
            var table = new StringTable();
            var lines = Gbk.ReadLines(stream);
            var count = lines.Length;

            if (count > 0 && lines[count - 1].Length == 0)
            {
                count--;
            }

            for (var i = 0; i < count; i++)
            {
                table.Add(lines[i]);
            }

            return table;
        }

        private void Add(string rawLine)
        {
            var line = rawLine.Replace("\\n", "\n").Replace("\\t", "\t");

            if (line.Length <= 7)
            {
                return;
            }

            var tab = line.IndexOf('\t');

            if (tab < 0)
            {
                return;
            }

            var id = line.Substring(0, tab);
            var text = line.Substring(tab + 1);

            if (id.Length > 2 && id[0] == '[' && id[^1] == ']' && text.Length > 2 && text[0] == '"' && text[^1] == '"')
            {
                var key = CText.Atoi(id.Substring(1, id.Length - 2));
                _strings[key] = text.Substring(1, text.Length - 2);
            }
        }
    }
}
