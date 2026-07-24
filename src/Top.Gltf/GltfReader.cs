using System;
using System.IO;
using System.Text;

namespace Top.Gltf
{
    public class GltfFile
    {
        public GltfDocument Document;
        public byte[] BinChunk;
    }

    public static class GltfReader
    {
        public static GltfFile Read(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            var bytes = ms.ToArray();

            if (bytes.Length >= 12 && BitConverter.ToUInt32(bytes, 0) == 0x46546C67)
            {
                return ReadGlb(bytes);
            }

            return new GltfFile
            {
                Document = GltfJson.Deserialize(Encoding.UTF8.GetString(bytes)),
            };
        }

        private static GltfFile ReadGlb(byte[] bytes)
        {
            var version = BitConverter.ToUInt32(bytes, 4);

            if (version != 2)
            {
                throw new InvalidDataException($"unsupported GLB version {version}");
            }

            var file = new GltfFile();
            var offset = 12;

            while (offset + 8 <= bytes.Length)
            {
                var chunkLength = BitConverter.ToInt32(bytes, offset);
                var chunkType = BitConverter.ToUInt32(bytes, offset + 4);
                var chunk = new byte[chunkLength];
                Array.Copy(bytes, offset + 8, chunk, 0, chunkLength);

                switch (chunkType)
                {
                    case 0x4E4F534A:
                        file.Document = GltfJson.Deserialize(Encoding.UTF8.GetString(chunk));
                        break;
                    case 0x004E4942:
                        file.BinChunk = chunk;
                        break;
                }

                offset += 8 + chunkLength;
            }

            return file.Document == null ? throw new InvalidDataException("GLB has no JSON chunk") : file;
        }
    }
}
