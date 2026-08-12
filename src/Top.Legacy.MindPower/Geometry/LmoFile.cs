using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// A .lmo model file.
    /// <br/> lwModelObjInfo::Load/Save (lwExpObj.cpp)
    /// </summary>
    public class LmoFile
    {
        private const uint VersionMin = 4096;
        private const uint VersionMax = 4101;
        private const uint GeometryObjectType = 1;
        private const uint HelperObjectType = 2;
        private const int ObjectTableEntryBytes = 12;

        public SceneModel Model;

        public static LmoFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var version = r.ReadUInt32();
            if (version != 0 && (version < VersionMin || version > VersionMax))
            {
                throw new ParseException("lmo", version, r.BaseStream.Position,
                    $"unsupported version 0x{version:X4}");
            }

            var objNum = r.ReadUInt32();
            var entries = new (uint Type, uint Addr, uint Size)[objNum];
            for (var i = 0; i < (int)objNum; i++)
            {
                entries[i] = (r.ReadUInt32(), r.ReadUInt32(), r.ReadUInt32());
            }

            var geometryObjects = new List<GeometryObject>();
            var helpers = new List<Helper>();

            foreach (var entry in entries)
            {
                r.BaseStream.Seek(entry.Addr, SeekOrigin.Begin);

                if (entry.Type == GeometryObjectType)
                {
                    if (version == 0)
                    {
                        r.ReadUInt32();
                    }

                    geometryObjects.Add(r.ReadGeometryObject(version));
                }
                else if (entry.Type == HelperObjectType)
                {
                    helpers.Add(r.ReadHelper(version));
                }
                else
                {
                    throw new ParseException("lmo", version, r.BaseStream.Position,
                        $"unsupported object type {entry.Type}");
                }
            }

            return new LmoFile
            {
                Model = new SceneModel
                {
                    Version = version,
                    GeometryObjects = geometryObjects.ToArray(),
                    Helpers = helpers.ToArray(),
                },
            };
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            var version = Model.Version;

            var blocks = new List<(uint Type, byte[] Data)>();
            foreach (var obj in Model.GeometryObjects)
            {
                blocks.Add((GeometryObjectType, GeometryObjectSerialization.Serialize(bw =>
                {
                    if (version == 0)
                    {
                        bw.Write(obj.Version);
                    }

                    bw.WriteGeometryObject(obj);
                })));
            }

            foreach (var helper in Model.Helpers)
            {
                blocks.Add((HelperObjectType,
                    GeometryObjectSerialization.Serialize(bw => bw.WriteHelper(helper, version))));
            }

            w.Write(version);
            w.Write((uint)blocks.Count);

            var addr = (uint)(8 + (ObjectTableEntryBytes * blocks.Count));
            foreach (var block in blocks)
            {
                w.Write(block.Type);
                w.Write(addr);
                w.Write((uint)block.Data.Length);
                addr += (uint)block.Data.Length;
            }

            foreach (var block in blocks)
            {
                w.Write(block.Data);
            }

            w.Flush();
        }
    }
}
