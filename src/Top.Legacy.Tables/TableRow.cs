using System;
using System.Numerics;
using Top.Legacy.Text;

namespace Top.Legacy.Tables
{
    /// <summary>
    /// Sequential cursor over one table row's columns. Reads past
    /// the last column yield empty strings.
    /// </summary>
    public class TableRow
    {
        private readonly string[] _fields;
        private int _next;

        public TableRow(int line, string[] fields)
        {
            _fields = fields;

            Line = line;
        }

        public int Line { get; }

        public string NextString()
        {
            var value = _next < _fields.Length ? _fields[_next] : string.Empty;
            _next++;

            return value;
        }

        public int NextInt()
        {
            return CText.Atoi(NextString());
        }

        public long NextLong()
        {
            return CText.AtoiLong(NextString());
        }

        public float NextFloat()
        {
            return CText.Atof(NextString());
        }

        public bool NextBool()
        {
            return NextInt() != 0;
        }

        public TEnum NextEnum<TEnum>() where TEnum : struct, Enum
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), NextInt());
        }

        public string[] NextStringList()
        {
            return TableText.SplitFields(NextString(), ',').ToArray();
        }

        public int[] NextIntList()
        {
            var parts = TableText.SplitFields(NextString(), ',');
            var values = new int[parts.Count];

            for (var i = 0; i < parts.Count; i++)
            {
                values[i] = CText.Atoi(parts[i]);
            }

            return values;
        }

        public float[] NextFloatList()
        {
            var parts = TableText.SplitFields(NextString(), ',');
            var values = new float[parts.Count];

            for (var i = 0; i < parts.Count; i++)
            {
                values[i] = CText.Atof(parts[i]);
            }

            return values;
        }

        public string[] NextStrings(int count)
        {
            var values = new string[count];

            for (var i = 0; i < count; i++)
            {
                values[i] = NextString();
            }

            return values;
        }

        public int[] NextInts(int count)
        {
            var values = new int[count];

            for (var i = 0; i < count; i++)
            {
                values[i] = NextInt();
            }

            return values;
        }

        public float[] NextFloats(int count)
        {
            var values = new float[count];

            for (var i = 0; i < count; i++)
            {
                values[i] = NextFloat();
            }

            return values;
        }

        public Vector3 NextVector3()
        {
            var parts = NextFloatList();

            return new Vector3(
                parts.Length > 0 ? parts[0] : 0f,
                parts.Length > 1 ? parts[1] : 0f,
                parts.Length > 2 ? parts[2] : 0f);
        }

        public void Skip(int count = 1)
        {
            _next += count;
        }
    }
}
