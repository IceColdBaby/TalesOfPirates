using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Top.Legacy.MindPower.Minimap
{
    /// <summary>
    /// A .pk minimap tile pack.
    /// <br/> CPackFile (PackFile.cpp)
    /// </summary>
    public class PkFile
    {
        public string Name;
        public PkEntry[] Files;
        public PkDirectory[] Directories;

        public static PkFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var root = r.ReadDirectory();
            return new PkFile { Name = root.Name, Files = root.Files, Directories = root.Directories, };
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            var root = new PkDirectory { Name = Name, Files = Files, Directories = Directories, };

            var cursor = (uint)PkSerialization.DirectoryByteSize(root);
            var offsets = new Queue<uint>();
            PkSerialization.CollectPayloadOffsets(root, ref cursor, offsets);

            w.WriteDirectory(root, offsets);
            w.WritePayloads(root);
            w.Flush();
        }
    }
}
