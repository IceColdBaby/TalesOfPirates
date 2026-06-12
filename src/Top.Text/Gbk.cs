using System;
using System.Text;

namespace Top.Text
{
    /// <summary>
    /// GBK (cp936) decoder backed by an embedded lead/trail table.
    /// </summary>
    public static class Gbk
    {
        private static readonly byte[] Table = Convert.FromBase64String(Gbk936Table.PackedBase64);

        public static string GetString(byte[] data)
        {
            return data == null ? string.Empty : GetString(data, 0, data.Length);
        }

        public static string GetString(byte[] data, int index, int count)
        {
            if (data == null || count <= 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(count);
            int end = index + count;
            int i = index;
            while (i < end)
            {
                byte b = data[i];
                if (b < 0x80)
                {
                    sb.Append((char)b);
                    i++;
                }
                else if (b == 0x80)
                {
                    sb.Append(Gbk936Table.Byte80);
                    i++;
                }
                else if (b <= 0xFE && i + 1 < end)
                {
                    byte t = data[i + 1];
                    if (t >= 0x40 && t <= 0xFE && t != 0x7F)
                    {
                        int idx = ((b - 0x81) * Gbk936Table.Trails + (t - 0x40)) * 2;
                        char c = (char)(Table[idx] | (Table[idx + 1] << 8));
                        if (c != 0)
                        {
                            sb.Append(c);
                            i += 2;
                            continue;
                        }
                    }

                    sb.Append('?');
                    i++;
                }
                else
                {
                    sb.Append('?');
                    i++;
                }
            }

            return sb.ToString();
        }

        private static System.Collections.Generic.Dictionary<char, ushort> _encode;

        public static byte[] GetBytes(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return new byte[0];
            }

            EnsureEncodeMap();
            var outp = new System.Collections.Generic.List<byte>(s.Length * 2);
            foreach (var c in s)
            {
                if (c < 0x80)
                {
                    outp.Add((byte)c);
                }
                else if (_encode.TryGetValue(c, out var pair))
                {
                    outp.Add((byte)(pair >> 8));
                    outp.Add((byte)pair);
                }
                else
                {
                    outp.Add((byte)'?');
                }
            }

            return outp.ToArray();
        }

        private static void EnsureEncodeMap()
        {
            if (_encode != null)
            {
                return;
            }

            var map = new System.Collections.Generic.Dictionary<char, ushort>(Gbk936Table.Trails * 0x7E);
            for (int lead = 0x81; lead <= 0xFE; lead++)
            {
                for (int trail = 0x40; trail <= 0xFE; trail++)
                {
                    if (trail == 0x7F)
                    {
                        continue;
                    }

                    int idx = ((lead - 0x81) * Gbk936Table.Trails + (trail - 0x40)) * 2;
                    char c = (char)(Table[idx] | (Table[idx + 1] << 8));
                    if (c != 0 && !map.ContainsKey(c))
                    {
                        map[c] = (ushort)((lead << 8) | trail);
                    }
                }
            }

            _encode = map;
        }
    }
}
