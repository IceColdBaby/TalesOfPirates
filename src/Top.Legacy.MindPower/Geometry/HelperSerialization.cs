using System.IO;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// Dummies, boxes, meshes, bounding boxes and spheres.
    /// <br/> lwHelperInfo::Load/Save (lwExpObj.cpp)
    /// </summary>
    internal static class HelperSerialization
    {
        internal static Helper ReadHelper(this BinaryReader r, uint version)
        {
            var helper = new Helper();

            if (version == 0)
            {
                r.ReadUInt32();
            }

            helper.Type = (HelperType)r.ReadUInt32();

            if ((helper.Type & HelperType.Dummy) != 0)
            {
                helper.Dummies = LoadHelperDummies(r, (int)r.ReadUInt32(), version);
            }

            if ((helper.Type & HelperType.Box) != 0)
            {
                helper.Boxes = LoadHelperBoxes(r, (int)r.ReadUInt32());
            }

            if ((helper.Type & HelperType.Mesh) != 0)
            {
                helper.Meshes = LoadHelperMeshes(r, (int)r.ReadUInt32());
            }

            if ((helper.Type & HelperType.BoundingBox) != 0)
            {
                helper.BoundingBoxes = LoadBoundingBoxes(r, (int)r.ReadUInt32());
            }

            if ((helper.Type & HelperType.BoundingSphere) != 0)
            {
                helper.BoundingSpheres = LoadBoundingSpheres(r, (int)r.ReadUInt32());
            }

            return helper;
        }

        internal static void WriteHelper(this BinaryWriter w, Helper helper, uint version)
        {
            if (version == 0)
            {
                w.Write(0u);
            }

            w.Write((uint)helper.Type);

            if ((helper.Type & HelperType.Dummy) != 0)
            {
                w.Write((uint)helper.Dummies.Length);
                WriteDummies(w, helper.Dummies, version);
            }

            if ((helper.Type & HelperType.Box) != 0)
            {
                w.Write((uint)helper.Boxes.Length);
                WriteBoxes(w, helper.Boxes);
            }

            if ((helper.Type & HelperType.Mesh) != 0)
            {
                w.Write((uint)helper.Meshes.Length);
                WriteMeshes(w, helper.Meshes);
            }

            if ((helper.Type & HelperType.BoundingBox) != 0)
            {
                w.Write((uint)helper.BoundingBoxes.Length);
                WriteBoundingBoxes(w, helper.BoundingBoxes);
            }

            if ((helper.Type & HelperType.BoundingSphere) != 0)
            {
                w.Write((uint)helper.BoundingSpheres.Length);
                WriteBoundingSpheres(w, helper.BoundingSpheres);
            }
        }

        private static HelperDummy[] LoadHelperDummies(BinaryReader r, int count, uint version)
        {
            var result = new HelperDummy[count];

            for (var i = 0; i < count; i++)
            {
                result[i].Id = r.ReadUInt32();
                result[i].Matrix = GeometryObjectSerialization.RowMajor(r.ReadMatrix44Raw());

                if (version < 4097)
                {
                    continue;
                }

                result[i].LocalMatrix = GeometryObjectSerialization.RowMajor(r.ReadMatrix44Raw());
                result[i].ParentType = r.ReadUInt32();
                result[i].ParentId = r.ReadUInt32();
            }

            return result;
        }

        private static HelperBox[] LoadHelperBoxes(BinaryReader r, int count)
        {
            var result = new HelperBox[count];

            for (var i = 0; i < count; i++)
            {
                result[i].Id = r.ReadUInt32();
                result[i].Type = r.ReadUInt32();
                result[i].State = r.ReadUInt32();
                result[i].BoxCenter = r.ReadVector3();
                result[i].BoxExtents = r.ReadVector3();
                result[i].Matrix = GeometryObjectSerialization.RowMajor(r.ReadMatrix44Raw());
                result[i].Name = r.ReadFixedString(32);
            }

            return result;
        }

        private static HelperMesh[] LoadHelperMeshes(BinaryReader r, int count)
        {
            var result = new HelperMesh[count];

            for (var i = 0; i < count; i++)
            {
                result[i].Id = r.ReadUInt32();
                result[i].Type = r.ReadUInt32();
                result[i].SubType = r.ReadUInt32();
                result[i].Name = r.ReadFixedString(32);
                result[i].State = r.ReadUInt32();
                result[i].Matrix = GeometryObjectSerialization.RowMajor(r.ReadMatrix44Raw());
                result[i].BoxCenter = r.ReadVector3();
                result[i].BoxExtents = r.ReadVector3();
                var vertexNum = r.ReadUInt32();
                var faceNum = r.ReadUInt32();
                result[i].Vertices = r.ReadVec3Array((int)vertexNum);
                result[i].Faces = LoadHelperMeshFaces(r, (int)faceNum);
            }

            return result;
        }

        private static HelperMeshFace[] LoadHelperMeshFaces(BinaryReader r, int count)
        {
            var results = new HelperMeshFace[count];

            for (var f = 0; f < count; f++)
            {
                results[f] = new HelperMeshFace { Vertex = new uint[3], AdjFace = new uint[3] };

                for (var i = 0; i < 3; i++)
                {
                    results[f].Vertex[i] = r.ReadUInt32();
                }

                for (var i = 0; i < 3; i++)
                {
                    results[f].AdjFace[i] = r.ReadUInt32();
                }

                results[f].Plane = r.ReadVector4();
                results[f].Center = r.ReadVector3();
            }

            return results;
        }

        private static BoundingBox[] LoadBoundingBoxes(BinaryReader r, int count)
        {
            var result = new BoundingBox[count];

            for (var i = 0; i < count; i++)
            {
                result[i].Id = r.ReadUInt32();
                result[i].BoxCenter = r.ReadVector3();
                result[i].BoxExtents = r.ReadVector3();
                result[i].Matrix = GeometryObjectSerialization.RowMajor(r.ReadMatrix44Raw());
            }

            return result;
        }

        private static BoundingSphere[] LoadBoundingSpheres(BinaryReader r, int count)
        {
            var result = new BoundingSphere[count];

            for (var i = 0; i < count; i++)
            {
                result[i].Id = r.ReadUInt32();
                result[i].Center = r.ReadVector3();
                result[i].Radius = r.ReadSingle();
                result[i].Matrix = GeometryObjectSerialization.RowMajor(r.ReadMatrix44Raw());
            }

            return result;
        }

        private static void WriteDummies(BinaryWriter w, HelperDummy[] dummies, uint version)
        {
            foreach (var d in dummies)
            {
                w.Write(d.Id);
                w.WriteMatrix44(d.Matrix);

                if (version < 4097)
                {
                    continue;
                }

                w.WriteMatrix44(d.LocalMatrix);
                w.Write(d.ParentType);
                w.Write(d.ParentId);
            }
        }

        private static void WriteBoxes(BinaryWriter w, HelperBox[] boxes)
        {
            foreach (var b in boxes)
            {
                w.Write(b.Id);
                w.Write(b.Type);
                w.Write(b.State);
                w.Write(b.BoxCenter);
                w.Write(b.BoxExtents);
                w.WriteMatrix44(b.Matrix);
                w.WriteFixedString(b.Name, 32);
            }
        }

        private static void WriteMeshes(BinaryWriter w, HelperMesh[] meshes)
        {
            foreach (var m in meshes)
            {
                w.Write(m.Id);
                w.Write(m.Type);
                w.Write(m.SubType);
                w.WriteFixedString(m.Name, 32);
                w.Write(m.State);
                w.WriteMatrix44(m.Matrix);
                w.Write(m.BoxCenter);
                w.Write(m.BoxExtents);
                w.Write((uint)m.Vertices.Length);
                w.Write((uint)m.Faces.Length);
                w.WriteVec3Array(m.Vertices);
                WriteMeshFaces(w, m.Faces);
            }
        }

        private static void WriteMeshFaces(BinaryWriter w, HelperMeshFace[] faces)
        {
            foreach (var f in faces)
            {
                for (var i = 0; i < 3; i++)
                {
                    w.Write(f.Vertex[i]);
                }

                for (var i = 0; i < 3; i++)
                {
                    w.Write(f.AdjFace[i]);
                }

                w.Write(f.Plane);
                w.Write(f.Center);
            }
        }

        private static void WriteBoundingBoxes(BinaryWriter w, BoundingBox[] boxes)
        {
            foreach (var b in boxes)
            {
                w.Write(b.Id);
                w.Write(b.BoxCenter);
                w.Write(b.BoxExtents);
                w.WriteMatrix44(b.Matrix);
            }
        }

        private static void WriteBoundingSpheres(BinaryWriter w, BoundingSphere[] spheres)
        {
            foreach (var s in spheres)
            {
                w.Write(s.Id);
                w.Write(s.Center);
                w.Write(s.Radius);
                w.WriteMatrix44(s.Matrix);
            }
        }
    }
}
