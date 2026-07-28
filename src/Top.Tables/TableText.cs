using System.Collections.Generic;

namespace Top.Tables
{
    /// <summary>
    /// Text primitives shared by all table parsers: GBK decoding and the
    /// C runtime parsing semantics (atoi/atof, Util_ResolveTextLine) the
    /// original readers rely on.
    /// </summary>
    public static class TableText
    {
        public static List<string> SplitFields(string text, char separator)
        {
            var fields = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                return fields;
            }

            if (text.Length == 1)
            {
                fields.Add(text);

                return fields;
            }

            var i = 0;

            while (true)
            {
                var found = text.IndexOf(separator, i);

                if (found < 0)
                {
                    fields.Add(text.Substring(i));

                    return fields;
                }

                fields.Add(text.Substring(i, found - i));
                i = found + 1;

                while (i < text.Length && text[i] == separator)
                {
                    i++;
                }

                if (i >= text.Length)
                {
                    return fields;
                }
            }
        }

        public static int CountLeadingNonZero(int[] values)
        {
            int n = 0;

            while (n < values.Length && values[n] != 0)
            {
                n++;
            }

            return n;
        }
    }
}
