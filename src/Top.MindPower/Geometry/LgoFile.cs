using System.IO;
using System.Text;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// A .lgo geometry-object file.
    /// <br/> lwGeomObjInfo::Load/Save (lwExpObj.cpp)
    /// </summary>
    public class LgoFile
    {
        private const uint VersionMin = 4096;
        private const uint VersionMax = 4101;

        public GeometryObject Object;

        public static LgoFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var version = r.ReadUInt32();
            if (version != 0 && (version < VersionMin || version > VersionMax))
            {
                throw new ParseException("lgo", version, r.BaseStream.Position,
                    $"unsupported version 0x{version:X4}");
            }

            return new LgoFile { Object = r.ReadGeometryObject(version) };
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write(Object.Version);
            w.WriteGeometryObject(Object);
            w.Flush();
        }
    }
}
