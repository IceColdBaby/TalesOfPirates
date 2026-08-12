using System;
using System.IO;
using System.Numerics;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// The header plus optional material / mesh / helper / animation sub-blocks.
    /// <br/> lwGeomObjInfo::Load/Save (lwExpObj.cpp)
    /// </summary>
    internal static class GeometryObjectSerialization
    {
        private const int RuntimeHeaderBytes = 16 + 8;

        internal static GeometryObject ReadGeometryObject(this BinaryReader r, uint version)
        {
            var id = r.ReadUInt32();
            var parentId = r.ReadUInt32();
            var type = r.ReadUInt32();
            var localMatrix = RowMajor(r.ReadMatrix44Raw());
            r.ReadBytes(RuntimeHeaderBytes);

            var mtlSize = r.ReadUInt32();
            var meshSize = r.ReadUInt32();
            var helperSize = r.ReadUInt32();
            var animSize = r.ReadUInt32();

            // lwGeomObjInfo::Load rejects implausible sizes at this point; a
            // few shipped files predate the header's rcci/state_ctrl fields
            // and are unloadable by the retail engine too.
            if (mtlSize > 100000)
            {
                throw new ParseException("geomobj", version, r.BaseStream.Position,
                    $"implausible material block size {mtlSize}; stale header layout");
            }

            var obj = new GeometryObject
            {
                Version = version,
                Id = id,
                ParentId = parentId,
                Type = type,
                LocalMatrix = localMatrix,
            };

            if (mtlSize > 0)
            {
                obj.MaterialVersion = r.ReadMaterialVersion(version);
                obj.Materials = r.ReadMaterials(obj.MaterialVersion);
            }

            obj.Mesh = meshSize > 0 ? r.ReadMesh(version) : null;
            obj.Helper = helperSize > 0 ? r.ReadHelper(version) : null;
            obj.Animation = animSize > 0 ? r.ReadAnimationData(version) : null;

            return obj;
        }

        internal static void WriteGeometryObject(this BinaryWriter w, GeometryObject obj)
        {
            var version = obj.Version;

            var mtl = obj.Materials != null
                ? Serialize(bw => bw.WriteMaterials(obj.Materials, obj.MaterialVersion, version))
                : null;
            var mesh = obj.Mesh != null ? Serialize(bw => bw.WriteMesh(obj.Mesh, version)) : null;
            var helper = obj.Helper != null ? Serialize(bw => bw.WriteHelper(obj.Helper, version)) : null;
            var anim = obj.Animation != null ? Serialize(bw => bw.WriteAnimationData(obj.Animation, version)) : null;

            w.Write(obj.Id);
            w.Write(obj.ParentId);
            w.Write(obj.Type);
            w.WriteMatrix44(obj.LocalMatrix);
            w.Write(new byte[RuntimeHeaderBytes]);

            w.Write((uint)(mtl?.Length ?? 0));
            w.Write((uint)(mesh?.Length ?? 0));
            w.Write((uint)(helper?.Length ?? 0));
            w.Write((uint)(anim?.Length ?? 0));

            WriteIfPresent(w, mtl);
            WriteIfPresent(w, mesh);
            WriteIfPresent(w, helper);
            WriteIfPresent(w, anim);
        }

        private static void WriteIfPresent(BinaryWriter w, byte[] block)
        {
            if (block != null)
            {
                w.Write(block);
            }
        }

        internal static byte[] Serialize(Action<BinaryWriter> write)
        {
            using var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                write(bw);
                bw.Flush();
            }

            return ms.ToArray();
        }

        internal static Matrix4x4 RowMajor(float[] m)
        {
            return new Matrix4x4(
                m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7],
                m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
        }
    }
}
