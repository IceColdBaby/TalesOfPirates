using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Text;

namespace Top.Tables.Custom
{
    public class LitEntry
    {
        public int ObjType;
        public int AnimType;
        public string File;
        public string Mask;
        public int SubId;
        public int ColorOp;
        public string[] Strings;
    }

    public class LitTable
    {
        private const string FileName = "lit.tx";

        public readonly List<LitEntry> Entries = new List<LitEntry>();

        public static LitTable Read(Stream stream)
        {
            var tokens = Tokenize(Gbk.ReadLines(stream));
            var pos = 0;
            var line = 0;

            string Next()
            {
                var t = tokens[pos];
                pos++;
                line = t.Line;
                return t.Text;
            }

            var table = new LitTable();

            // header label ("num:"); only the count matters
            Next();
            var count = CText.Atoi(Next());

            for (var i = 0; i < count; i++)
            {
                // per-entry label ("0)", "1)", ...)
                Next();
                var entry = new LitEntry { ObjType = CText.Atoi(Next()) };

                switch (entry.ObjType)
                {
                    // character
                    case 0:
                        entry.AnimType = CText.Atoi(Next());
                        entry.File = Next();
                        entry.Mask = Next();
                        entry.Strings = ReadStrings(CText.Atoi(Next()), Next);
                        break;
                    // scene
                    case 1:
                    // item
                    case 2:
                        entry.AnimType = CText.Atoi(Next());
                        entry.File = Next();
                        entry.SubId = CText.Atoi(Next());
                        entry.ColorOp = CText.Atoi(Next());
                        entry.Strings = ReadStrings(CText.Atoi(Next()), Next);
                        break;
                    default:
                        throw new TableFormatException(FileName, line,
                            $"invalid lit obj_type {entry.ObjType}");
                }

                table.Entries.Add(entry);
            }

            return table;
        }

        public LitEntry Find(int objType, int subId, string file)
        {
            foreach (var entry in Entries)
            {
                if (entry.ObjType == objType && string.Equals(entry.File, file, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        private static string[] ReadStrings(int count, Func<string> next)
        {
            var strings = new string[count];

            for (var i = 0; i < count; i++)
            {
                strings[i] = next();
            }

            return strings;
        }

        private static List<Token> Tokenize(string[] lines)
        {
            var tokens = new List<Token>();

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0)
                {
                    continue;
                }

                tokens.AddRange(lines[i]
                    .Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                    .Select(text => new Token { Text = text, Line = i + 1 }));
            }

            return tokens;
        }

        private class Token
        {
            public string Text;
            public int Line;
        }
    }
}
