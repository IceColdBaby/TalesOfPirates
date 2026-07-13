using System.Collections.Generic;
using System.IO;

namespace Top.MindPower.Minimap
{
    /// <summary>
    /// A .pk directory tree on disk.
    /// <br/> CPackFile::Load/Save (PackFile.cpp)
    /// </summary>
    internal static class PkSerialization
    {
        private static string ReadLengthPrefixedString(this BinaryReader r)
        {
            var length = r.ReadByte();
            var bytes = r.ReadBytes(length);

            return Text.Gbk.GetString(bytes, 0, bytes.Length);
        }

        private static void WriteLengthPrefixedString(this BinaryWriter w, string value)
        {
            var bytes = Text.Gbk.GetBytes(value ?? string.Empty);
            w.Write((byte)bytes.Length);
            w.Write(bytes);
        }

        internal static int DirectoryByteSize(PkDirectory directory)
        {
            var size = 1 + Text.Gbk.GetBytes(directory.Name ?? string.Empty).Length + 8;

            foreach (var file in directory.Files)
            {
                size += 1 + Text.Gbk.GetBytes(file.Name ?? string.Empty).Length + 8;
            }

            foreach (var sub in directory.Directories)
            {
                size += DirectoryByteSize(sub);
            }

            return size;
        }

        internal static PkDirectory ReadDirectory(this BinaryReader r)
        {
            var directory = new PkDirectory
            {
                Name = r.ReadLengthPrefixedString(),
            };

            var fileCount = r.ReadUInt32();
            var directoryCount = r.ReadUInt32();

            if (fileCount > int.MaxValue || directoryCount > int.MaxValue)
            {
                throw new ParseException("pk", 0, r.BaseStream.Position,
                    $"implausible counts files {fileCount} dirs {directoryCount}");
            }

            directory.Files = new PkEntry[fileCount];

            var offsets = new uint[fileCount];
            var sizes = new uint[fileCount];

            for (var i = 0; i < fileCount; i++)
            {
                directory.Files[i] = new PkEntry { Name = r.ReadLengthPrefixedString() };
                offsets[i] = r.ReadUInt32();
                sizes[i] = r.ReadUInt32();
            }

            directory.Directories = new PkDirectory[directoryCount];

            for (var i = 0; i < directoryCount; i++)
            {
                directory.Directories[i] = r.ReadDirectory();
            }

            for (var i = 0; i < fileCount; i++)
            {
                if (offsets[i] + sizes[i] > r.BaseStream.Length)
                {
                    throw new ParseException("pk", 0, offsets[i],
                        $"payload [{offsets[i]}, {offsets[i] + sizes[i]}) exceeds file length {r.BaseStream.Length}");
                }

                r.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
                directory.Files[i].Payload = r.ReadBytes((int)sizes[i]);
            }

            return directory;
        }

        internal static void WriteDirectory(this BinaryWriter w, PkDirectory directory, Queue<uint> offsets)
        {
            w.WriteLengthPrefixedString(directory.Name);
            w.Write((uint)directory.Files.Length);
            w.Write((uint)directory.Directories.Length);

            foreach (var file in directory.Files)
            {
                w.WriteLengthPrefixedString(file.Name);
                w.Write(offsets.Dequeue());
                w.Write((uint)file.Payload.Length);
            }

            foreach (var sub in directory.Directories)
            {
                w.WriteDirectory(sub, offsets);
            }
        }

        internal static void CollectPayloadOffsets(PkDirectory directory, ref uint cursor, Queue<uint> offsets)
        {
            foreach (var file in directory.Files)
            {
                offsets.Enqueue(cursor);
                cursor += (uint)file.Payload.Length;
            }

            foreach (var sub in directory.Directories)
            {
                CollectPayloadOffsets(sub, ref cursor, offsets);
            }
        }

        internal static void WritePayloads(this BinaryWriter w, PkDirectory directory)
        {
            foreach (var file in directory.Files)
            {
                w.Write(file.Payload);
            }

            foreach (var sub in directory.Directories)
            {
                w.WritePayloads(sub);
            }
        }
    }
}
