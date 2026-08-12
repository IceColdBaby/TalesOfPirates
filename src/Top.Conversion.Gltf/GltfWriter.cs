using System;
using System.IO;
using System.Text;

namespace Top.Conversion.Gltf
{
    public static class GltfWriter
    {
        private const uint Magic = 0x46546C67;
        private const uint JsonChunk = 0x4E4F534A;
        private const uint BinChunk = 0x004E4942;

        public static void WriteGlb(GltfDocument document, byte[] bin, Stream stream)
        {
            document.Buffers[0].Uri = null;
            document.Buffers[0].ByteLength = bin.Length;

            var json = Pad(Encoding.UTF8.GetBytes(GltfJson.Serialize(document, indented: false)), 0x20);
            var binPadded = Pad(bin, 0x00);

            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            var total = 12 + 8 + json.Length + (bin.Length > 0 ? 8 + binPadded.Length : 0);

            w.Write(Magic);
            w.Write(2u);
            w.Write((uint)total);
            w.Write((uint)json.Length);
            w.Write(JsonChunk);
            w.Write(json);

            if (bin.Length > 0)
            {
                w.Write((uint)binPadded.Length);
                w.Write(BinChunk);
                w.Write(binPadded);
            }

            w.Flush();
        }

        public static void WriteGltfEmbedded(GltfDocument document, byte[] bin, Stream stream)
        {
            document.Buffers[0].Uri = "data:application/octet-stream;base64," + Convert.ToBase64String(bin);
            document.Buffers[0].ByteLength = bin.Length;
            WriteJson(document, stream);
        }

        public static void WriteGltfWithBin(GltfDocument document, byte[] bin, string binUri,
            Stream jsonStream, Stream binStream)
        {
            document.Buffers[0].Uri = binUri;
            document.Buffers[0].ByteLength = bin.Length;
            WriteJson(document, jsonStream);
            binStream.Write(bin, 0, bin.Length);
        }

        private static void WriteJson(GltfDocument document, Stream stream)
        {
            var bytes = Encoding.UTF8.GetBytes(GltfJson.Serialize(document, indented: true));
            stream.Write(bytes, 0, bytes.Length);
        }

        private static byte[] Pad(byte[] data, byte filler)
        {
            var padded = (data.Length + 3) / 4 * 4;

            if (padded == data.Length)
            {
                return data;
            }

            var result = new byte[padded];
            data.CopyTo(result, 0);

            for (var i = data.Length; i < padded; i++)
            {
                result[i] = filler;
            }

            return result;
        }
    }
}
