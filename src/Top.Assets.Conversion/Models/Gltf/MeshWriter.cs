using System;
using System.Collections.Generic;
using System.Numerics;
using Top.Assets.Contract.Models;
using Top.Gltf;
using Top.Logging;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Models.Gltf
{
    /// <summary>
    /// The joints and weights accessors a skinned mesh binds.
    /// </summary>
    public class SkinAttributes
    {
        public int Joints;
        public int Weights;
    }

    /// <summary>
    /// What one geometry object became in the document.
    /// </summary>
    public class GeometryMeshes
    {
        public GltfMeshRef Mesh;
        public GltfMeshRef LitMesh;
    }

    /// <summary>
    /// Writes the geometry of one model into a glTF document, asking
    /// <see cref="MaterialWriter"/> for the material each subset binds.
    /// </summary>
    public class MeshWriter
    {
        private readonly GltfBuilder _gltf;
        private readonly MaterialWriter _materials;
        private readonly int? _litSubset;

        public MeshWriter(GltfBuilder gltf, MaterialWriter materials, int? litSubset = null)
        {
            _gltf = gltf;
            _materials = materials;
            _litSubset = litSubset;
        }

        public SkinAttributes AddSkinAttributes(Mesh mesh)
        {
            var joints = new ushort[mesh.SkinBlends.Length * 4];
            var weights = new Vector4[mesh.SkinBlends.Length];

            for (var v = 0; v < mesh.SkinBlends.Length; v++)
            {
                var blend = mesh.SkinBlends[v];
                var w = new[] { blend.Weight0, blend.Weight1, blend.Weight2, blend.Weight3 };
                weights[v] = new Vector4(w[0], w[1], w[2], w[3]);

                for (var k = 0; k < 4; k++)
                {
                    var slot = (int)((blend.BoneIndex >> (8 * k)) & 0xFF);

                    joints[(v * 4) + k] = w[k] > 0f && slot < mesh.BoneIndices.Length
                        ? (ushort)mesh.BoneIndices[slot]
                        : (ushort)0;
                }
            }

            return new SkinAttributes
            {
                Joints = _gltf.Buffer.AddJoints(joints),
                Weights = _gltf.Buffer.AddVec4(weights, vertexData: true),
            };
        }

        public GeometryMeshes Add(GeometryObject obj, string name, SkinAttributes attributes = null)
        {
            var mesh = obj.Mesh;
            var positions = new Vector3[mesh.Vertices.Length];

            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = Axis.ToGltf(mesh.Vertices[i]);
            }

            var positionAccessor = _gltf.Buffer.AddVec3(positions, withMinMax: true);

            int? normalAccessor = null;

            if (mesh.Normals != null && mesh.Normals.Length > 0)
            {
                var normals = new Vector3[mesh.Normals.Length];
                var degenerate = 0;

                for (var i = 0; i < normals.Length; i++)
                {
                    var normal = mesh.Normals[i];
                    var lengthSquared = normal.LengthSquared();

                    if (!float.IsFinite(lengthSquared) || lengthSquared < 1e-12f)
                    {
                        normal = Vector3.UnitZ;
                        degenerate++;
                    }
                    else
                    {
                        normal /= MathF.Sqrt(lengthSquared);
                    }

                    normals[i] = Axis.ToGltf(normal);
                }

                if (degenerate > 0)
                {
                    Log.Warning($"object {obj.Id} has {degenerate} degenerate normals, replaced");
                }

                normalAccessor = _gltf.Buffer.AddVec3(normals, withMinMax: false);
            }

            int? uvAccessor = null;

            if (mesh.TextureCoordinates != null && mesh.TextureCoordinates.Length > 0 &&
                mesh.TextureCoordinates[0] != null)
            {
                uvAccessor = _gltf.Buffer.AddVec2(mesh.TextureCoordinates[0]);
            }

            int? colorAccessor = null;

            if (mesh.VertexColors != null && mesh.VertexColors.Length > 0)
            {
                var rgba = new byte[mesh.VertexColors.Length * 4];

                for (var i = 0; i < mesh.VertexColors.Length; i++)
                {
                    var c = mesh.VertexColors[i];
                    rgba[(i * 4) + 0] = (byte)(c >> 16);
                    rgba[(i * 4) + 1] = (byte)(c >> 8);
                    rgba[(i * 4) + 2] = (byte)c;
                    rgba[(i * 4) + 3] = (byte)(c >> 24);
                }

                colorAccessor = _gltf.Buffer.AddColors(rgba, mesh.VertexColors.Length);
            }

            var gltfMesh = _gltf.AddMesh(name);
            GltfMeshRef litMesh = null;

            for (var subsetIndex = 0; subsetIndex < mesh.Subsets.Length; subsetIndex++)
            {
                var subset = mesh.Subsets[subsetIndex];
                var indices = new uint[subset.PrimitiveCount * 3];

                for (var t = 0; t < (int)subset.PrimitiveCount; t++)
                {
                    var src = subset.StartIndex + (uint)(t * 3);
                    indices[(t * 3) + 0] = mesh.Indices[src];
                    indices[(t * 3) + 1] = mesh.Indices[src + 2];
                    indices[(t * 3) + 2] = mesh.Indices[src + 1];
                }

                var primitive = new Dictionary<string, int> { ["POSITION"] = positionAccessor };

                if (normalAccessor != null)
                {
                    primitive["NORMAL"] = normalAccessor.Value;
                }

                if (uvAccessor != null)
                {
                    primitive["TEXCOORD_0"] = uvAccessor.Value;
                }

                if (colorAccessor != null)
                {
                    primitive["COLOR_0"] = colorAccessor.Value;
                }

                if (attributes != null)
                {
                    primitive["JOINTS_0"] = attributes.Joints;
                    primitive["WEIGHTS_0"] = attributes.Weights;
                }

                var material = _materials.Add(obj, subsetIndex);
                var indicesAccessor = _gltf.Buffer.AddIndices(indices);

                if (subsetIndex == _litSubset && mesh.Subsets.Length > 1)
                {
                    litMesh ??= _gltf.AddMesh(Naming.LitShell(name));
                    litMesh.AddPrimitive(primitive, indicesAccessor, material);
                }
                else
                {
                    gltfMesh.AddPrimitive(primitive, indicesAccessor, material);
                }
            }

            return new GeometryMeshes { Mesh = gltfMesh, LitMesh = litMesh };
        }

        public GltfMeshRef AddHelper(HelperMesh helperMesh, string name)
        {
            var positions = new Vector3[helperMesh.Vertices.Length];

            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = Axis.ToGltf(helperMesh.Vertices[i]);
            }

            var indices = new uint[helperMesh.Faces.Length * 3];

            for (var f = 0; f < helperMesh.Faces.Length; f++)
            {
                indices[(f * 3) + 0] = helperMesh.Faces[f].Vertex[0];
                indices[(f * 3) + 1] = helperMesh.Faces[f].Vertex[2];
                indices[(f * 3) + 2] = helperMesh.Faces[f].Vertex[1];
            }

            return _gltf
                .AddMesh(name)
                .AddPrimitive(
                    new Dictionary<string, int>
                    {
                        ["POSITION"] = _gltf.Buffer.AddVec3(positions, withMinMax: true),
                    },
                    _gltf.Buffer.AddIndices(indices));
        }
    }
}
