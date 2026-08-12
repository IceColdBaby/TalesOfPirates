using System.IO;
using System.Text;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// A .lxo scene-tree file.
    /// <br/> lwModelInfo::Load/Save (lwExpObj.cpp)
    /// </summary>
    public class LxoFile
    {
        private const uint VersionMin = 4096;
        private const uint VersionMax = 4101;
        private const int DescriptorBytes = 64;
        private const string Descriptor = "lwModelInfo";

        public SceneTree Tree;

        public static LxoFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var mask = r.ReadUInt32();
            var version = r.ReadUInt32();
            if (version < VersionMin || version > VersionMax)
            {
                throw new ParseException("lxo", version, r.BaseStream.Position,
                    $"unsupported version 0x{version:X4}");
            }

            r.ReadBytes(DescriptorBytes);

            var objNum = r.ReadUInt32();
            var nodes = new SceneNode[objNum];
            for (var i = 0; i < (int)objNum; i++)
            {
                nodes[i] = r.ReadSceneNode(version);
            }

            return new LxoFile
            {
                Tree = new SceneTree { Mask = mask, Version = version, Nodes = nodes },
            };
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write(Tree.Mask);
            w.Write(Tree.Version);
            w.WriteFixedString(Descriptor, DescriptorBytes);

            w.Write((uint)Tree.Nodes.Length);
            foreach (var node in Tree.Nodes)
            {
                w.WriteSceneNode(node, Tree.Version);
            }

            w.Flush();
        }
    }
}
