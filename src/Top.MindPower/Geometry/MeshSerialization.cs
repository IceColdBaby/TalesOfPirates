using System.IO;
using System.Numerics;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// The mesh sub-block of an .lgo/.lmo geometry object.
    /// <br/> lwMeshInfo::Load/Save (lwExpObj.cpp)
    /// </summary>
    internal static class MeshSerialization
    {
        internal static Mesh ReadMesh(this BinaryReader r, uint version)
        {
            var mesh = new Mesh { LegacyMeshVersion = -1 };

            var rsSetBytes = 96;
            if (version == 0)
            {
                mesh.LegacyMeshVersion = (int)r.ReadUInt32();
                rsSetBytes = mesh.LegacyMeshVersion == 0 ? 128 : 96;
            }

            var v4Plus = version >= 4100;

            mesh.Fvf = r.ReadUInt32();
            mesh.PointType = r.ReadUInt32();
            var vertexNum = r.ReadUInt32();
            var indexNum = r.ReadUInt32();
            var subsetNum = r.ReadUInt32();
            var boneIndexNum = r.ReadUInt32();
            mesh.BoneInfluenceFactor = v4Plus ? r.ReadUInt32() : 0u;
            var vertexElementNum = v4Plus ? r.ReadUInt32() : 0u;

            mesh.RenderStateSet = rsSetBytes == 128 ? r.ReadLegacyRenderStates() : r.ReadRenderStateAtoms();

            var hasNormal = (mesh.Fvf & FvfFlags.Normal) != 0;
            var hasDiffuse = (mesh.Fvf & FvfFlags.Diffuse) != 0;
            var texCount = (int)((mesh.Fvf & FvfFlags.TexMask) >> 8);
            var hasSkin = v4Plus
                ? boneIndexNum > 0
                : (mesh.Fvf & FvfFlags.LastBetaUbyte4) != 0;

            if (!v4Plus)
            {
                mesh.Subsets = r.ReadSubsets((int)subsetNum);
            }
            else if (vertexElementNum > 0)
            {
                mesh.VertexElements = r.ReadVertexElements((int)vertexElementNum);
            }

            mesh.Vertices = r.ReadVec3Array((int)vertexNum);
            mesh.Normals = hasNormal ? r.ReadVec3Array((int)vertexNum) : null;

            if (texCount > 0)
            {
                mesh.TextureCoordinates = new Vector2[texCount][];
                for (var t = 0; t < texCount; t++)
                {
                    mesh.TextureCoordinates[t] = r.ReadVec2Array((int)vertexNum);
                }
            }

            mesh.VertexColors = hasDiffuse ? r.ReadUintArray((int)vertexNum) : null;

            if (hasSkin)
            {
                mesh.SkinBlends = r.ReadBlendData((int)vertexNum);
                mesh.BoneIndices = r.ReadBoneIndices((int)boneIndexNum, v4Plus);
            }

            mesh.Indices = r.ReadUintArray((int)indexNum);

            if (v4Plus)
            {
                mesh.Subsets = r.ReadSubsets((int)subsetNum);
            }

            return mesh;
        }

        internal static void WriteMesh(this BinaryWriter w, Mesh mesh, uint version)
        {
            var legacyRsSet = false;
            if (version == 0)
            {
                w.Write((uint)mesh.LegacyMeshVersion);
                legacyRsSet = mesh.LegacyMeshVersion == 0;
            }

            var v4Plus = version >= 4100;
            var vertexNum = (uint)(mesh.Vertices?.Length ?? 0);
            var boneIndexNum = (uint)(mesh.BoneIndices?.Length ?? 0);

            w.Write(mesh.Fvf);
            w.Write(mesh.PointType);
            w.Write(vertexNum);
            w.Write((uint)(mesh.Indices?.Length ?? 0));
            w.Write((uint)(mesh.Subsets?.Length ?? 0));
            w.Write(boneIndexNum);

            if (v4Plus)
            {
                w.Write(mesh.BoneInfluenceFactor);
                w.Write((uint)(mesh.VertexElements?.Length ?? 0));
            }

            if (legacyRsSet)
            {
                w.WriteLegacyRenderStates(mesh.RenderStateSet);
            }
            else
            {
                w.WriteRenderStateAtoms(mesh.RenderStateSet);
            }

            var hasNormal = (mesh.Fvf & FvfFlags.Normal) != 0;
            var hasDiffuse = (mesh.Fvf & FvfFlags.Diffuse) != 0;
            var texCount = (int)((mesh.Fvf & FvfFlags.TexMask) >> 8);
            var hasSkin = v4Plus ? boneIndexNum > 0 : (mesh.Fvf & FvfFlags.LastBetaUbyte4) != 0;

            if (!v4Plus)
            {
                w.WriteSubsets(mesh.Subsets);
            }
            else if (mesh.VertexElements is { Length: > 0 })
            {
                w.WriteVertexElements(mesh.VertexElements);
            }

            w.WriteVec3Array(mesh.Vertices);

            if (hasNormal)
            {
                w.WriteVec3Array(mesh.Normals);
            }

            for (var t = 0; t < texCount; t++)
            {
                w.WriteVec2Array(mesh.TextureCoordinates[t]);
            }

            if (hasDiffuse)
            {
                w.WriteUintArray(mesh.VertexColors);
            }

            if (hasSkin)
            {
                w.WriteBlendData(mesh.SkinBlends);
                w.WriteBoneIndices(mesh.BoneIndices, v4Plus);
            }

            w.WriteUintArray(mesh.Indices);

            if (v4Plus)
            {
                w.WriteSubsets(mesh.Subsets);
            }
        }

        private static MeshSubset[] ReadSubsets(this BinaryReader r, int count)
        {
            var subsets = new MeshSubset[count];

            for (var i = 0; i < count; i++)
            {
                subsets[i].PrimitiveCount = r.ReadUInt32();
                subsets[i].StartIndex = r.ReadUInt32();
                subsets[i].VertexCount = r.ReadUInt32();
                subsets[i].MinIndex = r.ReadUInt32();
            }

            return subsets;
        }

        private static VertexElement[] ReadVertexElements(this BinaryReader r, int count)
        {
            var arr = new VertexElement[count];

            for (var i = 0; i < count; i++)
            {
                arr[i].Stream = r.ReadUInt16();
                arr[i].Offset = r.ReadUInt16();
                arr[i].Type = r.ReadByte();
                arr[i].Method = r.ReadByte();
                arr[i].Usage = r.ReadByte();
                arr[i].UsageIndex = r.ReadByte();
            }

            return arr;
        }

        private static SkinBlend[] ReadBlendData(this BinaryReader r, int vertexNum)
        {
            var arr = new SkinBlend[vertexNum];

            for (var i = 0; i < vertexNum; i++)
            {
                arr[i].BoneIndex = r.ReadUInt32();
                arr[i].Weight0 = r.ReadSingle();
                arr[i].Weight1 = r.ReadSingle();
                arr[i].Weight2 = r.ReadSingle();
                arr[i].Weight3 = r.ReadSingle();
            }

            return arr;
        }

        private static uint[] ReadBoneIndices(this BinaryReader r, int boneIdxNum, bool v4Plus)
        {
            var arr = new uint[boneIdxNum];

            if (v4Plus)
            {
                for (var i = 0; i < boneIdxNum; i++)
                {
                    arr[i] = r.ReadUInt32();
                }
            }
            else
            {
                for (var i = 0; i < boneIdxNum; i++)
                {
                    arr[i] = r.ReadByte();
                }
            }

            return arr;
        }

        private static void WriteSubsets(this BinaryWriter w, MeshSubset[] subsets)
        {
            foreach (var s in subsets)
            {
                w.Write(s.PrimitiveCount);
                w.Write(s.StartIndex);
                w.Write(s.VertexCount);
                w.Write(s.MinIndex);
            }
        }

        private static void WriteVertexElements(this BinaryWriter w, VertexElement[] elements)
        {
            foreach (var e in elements)
            {
                w.Write(e.Stream);
                w.Write(e.Offset);
                w.Write(e.Type);
                w.Write(e.Method);
                w.Write(e.Usage);
                w.Write(e.UsageIndex);
            }
        }

        private static void WriteBlendData(this BinaryWriter w, SkinBlend[] blend)
        {
            foreach (var b in blend)
            {
                w.Write(b.BoneIndex);
                w.Write(b.Weight0);
                w.Write(b.Weight1);
                w.Write(b.Weight2);
                w.Write(b.Weight3);
            }
        }

        private static void WriteBoneIndices(this BinaryWriter w, uint[] boneIndices, bool v4Plus)
        {
            if (v4Plus)
            {
                foreach (var b in boneIndices)
                {
                    w.Write(b);
                }
            }
            else
            {
                foreach (var b in boneIndices)
                {
                    w.Write((byte)b);
                }
            }
        }
    }
}
