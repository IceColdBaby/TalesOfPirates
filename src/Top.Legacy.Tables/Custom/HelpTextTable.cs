using System.IO;
using Top.Legacy.Text;

namespace Top.Legacy.Tables.Custom
{
    public class HelpTextTable
    {
        private const int MaxSize = 4096;

        public string Text;

        public static HelpTextTable Read(Stream stream)
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var raw = buffer.ToArray();

            var translated = new byte[raw.Length];
            var count = 0;

            for (var i = 0; i < raw.Length; i++)
            {
                if (raw[i] == (byte)'\r' && i + 1 < raw.Length && raw[i + 1] == (byte)'\n')
                {
                    continue;
                }

                translated[count++] = raw[i];
            }

            if (count > MaxSize - 1)
            {
                count = MaxSize - 1;
            }

            return new HelpTextTable { Text = Gbk.GetString(translated, 0, count) };
        }
    }
}
