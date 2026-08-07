using System;
using System.Collections.Generic;
using System.IO;
using Top.Text;

namespace Top.Tables.Custom
{
    public struct CharacterAction
    {
        public int ActionNo;
        public int StartFrame;
        public int EndFrame;
        public int[] KeyFrames;
    }

    public class CharacterActionTable
    {
        private static readonly char[] Separators = { ' ', ',', '\t' };

        private readonly Dictionary<int, Dictionary<int, CharacterAction>> _types =
            new Dictionary<int, Dictionary<int, CharacterAction>>();

        public static CharacterActionTable Read(Stream stream)
        {
            var table = new CharacterActionTable();

            Dictionary<int, CharacterAction> current = null;

            foreach (var line in Gbk.ReadLines(stream))
            {
                var first = 0;

                while (first < line.Length && line[first] == ' ')
                {
                    first++;
                }

                if (first >= line.Length)
                {
                    continue;
                }

                if (line[first] == '/' && first + 1 < line.Length && line[first + 1] == '/')
                {
                    continue;
                }

                var tokens = line.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

                if (line[first] != '\t')
                {
                    if (tokens.Length == 0 || !int.TryParse(tokens[0], out var type) || type < 1)
                    {
                        current = null;
                        continue;
                    }

                    current = new Dictionary<int, CharacterAction>();
                    table._types[type] = current;
                    continue;
                }

                if (current == null)
                {
                    continue;
                }

                if (tokens.Length < 3
                    || !int.TryParse(tokens[0], out var actionNo)
                    || !int.TryParse(tokens[1], out var start)
                    || !int.TryParse(tokens[2], out var end))
                {
                    continue;
                }

                if (actionNo < 1)
                {
                    continue;
                }

                var keys = new List<int>();

                for (var k = 3; k < tokens.Length; k++)
                {
                    if (int.TryParse(tokens[k], out var key))
                    {
                        keys.Add(key);
                    }
                }

                current[actionNo] = new CharacterAction
                {
                    ActionNo = actionNo,
                    StartFrame = start,
                    EndFrame = end,
                    KeyFrames = keys.ToArray(),
                };
            }

            return table;
        }

        public bool TryGetActions(int characterType, out CharacterAction[] actions)
        {
            actions = null;

            if (!_types.TryGetValue(characterType, out var rows))
            {
                return false;
            }

            var list = new List<CharacterAction>();

            foreach (var row in rows.Values)
            {
                if (row.StartFrame == 0 && row.EndFrame == 0)
                {
                    continue;
                }

                list.Add(row);
            }

            if (list.Count == 0)
            {
                return false;
            }

            list.Sort((a, b) => a.ActionNo.CompareTo(b.ActionNo));
            actions = list.ToArray();

            return true;
        }
    }
}
