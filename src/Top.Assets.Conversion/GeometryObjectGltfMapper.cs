using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Top.Assets.Conversion.Materials;
using Top.Gltf;
using Top.MindPower.Animation;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion
{
    public class GltfConversion
    {
        public GltfDocument Document;
        public byte[] Bin;
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// Projects geometry objects (a .lmo scene model or a single .lgo object)
    /// into a glTF document.
    /// Axes: ToP is Z-up left-handed, glTF is Y-up right-handed. Swapping Y and
    /// Z maps up to +Y and flips handedness, so triangle winding is reversed.
    /// Emits geometry plus non-authoritative preview materials (texture
    /// bindings for DCC display); authoritative render state lives in Unity
    /// assets and the document never carries extras. Preview materials are
    /// emitted one per mesh subset, matching the scaffolded material assets
    /// they are remapped to.
    /// A lit-subset index splits that subset into a child lit shell node
    /// (item models' forge-glow shell, which the game shows only on socketed
    /// items) so consumers can toggle it separately. Skinned scene objects
    /// carry their skeleton in a sibling container subtree and animate
    /// through a clip named after the object.
    /// Every name emitted here comes from <see cref="GltfContract"/>.
    /// </summary>
    public static class GeometryObjectGltfMapper
    {
        public static GltfConversion Map(SceneModel model, string name, string textureUriPrefix = null)
        {
            return Map(model.GeometryObjects, model.Helpers, name, textureUriPrefix, litSubset: null);
        }

        public static GltfConversion Map(GeometryObject obj, string name, string textureUriPrefix = null,
            int? litSubset = null)
        {
            return Map(new[] { obj },
                obj.Helper == null ? Array.Empty<Helper>() : new[] { obj.Helper },
                name, textureUriPrefix, litSubset);
        }

        public static GltfConversion Map(GeometryObject obj, BoneAnimation skeleton, string name,
            string textureUriPrefix = null)
        {
            var b = new GltfBuilder(name, GltfMapping.Generator);
            var warnings = new List<string>();
            var boneNodes = GltfMapping.AddSkeleton(b, skeleton, warnings);
            var mesh = obj.Mesh;
            var skinned = HasBlendData(mesh);
            int? jointsAccessor = null;
            int? weightsAccessor = null;

            if (skinned)
            {
                (jointsAccessor, weightsAccessor) = AddSkinAttributes(b.Buffer, mesh);
            }
            else
            {
                warnings.Add($"object {obj.Id} carries no blend data; mesh stays rigid");
            }

            var nodeName = GltfContract.Geometry(obj.Id);
            var (meshRef, _) = AddGeometryMesh(b, obj, nodeName, name, textureUriPrefix,
                litSubset: null, warnings, jointsAccessor, weightsAccessor);
            var meshNode = b.AddNode(nodeName).WithMesh(meshRef);

            if (skinned)
            {
                meshNode.WithSkin(AddSkin(b, skeleton, boneNodes, name));
            }
            else if (!obj.LocalMatrix.IsIdentity)
            {
                meshNode.WithMatrix(GltfMapping.ToGltfMatrix(GltfMapping.ToGltf(obj.LocalMatrix)));
            }

            meshNode.AsRoot();

            AddHelperNodes(b, obj.Helper == null ? Array.Empty<Helper>() : new[] { obj.Helper });

            return Finish(b, warnings);
        }

        private static GltfConversion Map(GeometryObject[] objects, Helper[] helpers,
            string name, string textureUriPrefix, int? litSubset)
        {
            var b = new GltfBuilder(name, GltfMapping.Generator);
            var warnings = new List<string>();
            var nodeByObjectId = new Dictionary<uint, GltfNodeRef>();
            var skinnedObjects =
                new List<(GeometryObject Object, BoneAnimation Skeleton, GltfNodeRef[] BoneNodes)>();

            foreach (var obj in objects)
            {
                var skeleton = obj.Animation?.Bone;
                var hasBlends = HasBlendData(obj.Mesh);
                var skinned = skeleton != null && hasBlends;

                if (skeleton == null && obj.Mesh.SkinBlends != null && obj.Mesh.SkinBlends.Length > 0)
                {
                    warnings.Add($"skinning data on object {obj.Id} ignored");
                }

                if (skeleton != null && !hasBlends)
                {
                    warnings.Add($"bone animation on object {obj.Id} ignored; no blend data");
                }

                if (obj.Mesh.PointType != 4)
                {
                    warnings.Add($"object {obj.Id} uses primitive type {obj.Mesh.PointType}; " +
                        "only triangle lists convert");
                }

                int? jointsAccessor = null;
                int? weightsAccessor = null;

                if (skinned)
                {
                    (jointsAccessor, weightsAccessor) = AddSkinAttributes(b.Buffer, obj.Mesh);
                }

                var nodeName = GltfContract.Geometry(obj.Id);
                var (meshRef, litMeshRef) = AddGeometryMesh(b, obj, nodeName, name, textureUriPrefix,
                    litSubset, warnings, jointsAccessor, weightsAccessor);
                var node = b.AddNode(nodeName).WithMesh(meshRef);
                var animated = obj.Animation?.Matrix?.Frames != null && obj.Animation.Matrix.Frames.Length > 0;

                if (skinned)
                {
                    // glTF ignores a skinned node's own transform: the mesh
                    // follows its bones instead.
                    if (!obj.LocalMatrix.IsIdentity)
                    {
                        warnings.Add($"object {obj.Id} is skinned; its local matrix is ignored");
                    }

                    // Both animations would otherwise be emitted as a clip
                    // named geom_<id>, colliding on import. Bone animation
                    // wins: that is what the engine's per-object animation
                    // controller drives for a skinned object.
                    if (animated)
                    {
                        warnings.Add($"object {obj.Id} has both bone and matrix animation; " +
                            "the matrix animation is ignored");
                    }
                }
                else if (animated && Matrix4x4.Decompose(GltfMapping.ToGltf(obj.LocalMatrix),
                        out var nodeScale, out var nodeRotation, out var nodeTranslation))
                {
                    node.WithTrs(nodeTranslation, nodeRotation, nodeScale);
                }
                else if (!obj.LocalMatrix.IsIdentity)
                {
                    node.WithMatrix(GltfMapping.ToGltfMatrix(GltfMapping.ToGltf(obj.LocalMatrix)));
                }

                if (nodeByObjectId.ContainsKey(obj.Id))
                {
                    // Ids are the only handle the scaffolder has on a node, so
                    // a repeat leaves the earlier object's animation, material
                    // and track bindings pointing at this one.
                    warnings.Add($"object id {obj.Id} occurs more than once; " +
                        "the later object takes the name and its bindings");
                }

                nodeByObjectId[obj.Id] = node;

                if (litMeshRef != null)
                {
                    node.AddChild(b.AddNode(GltfContract.LitShell(nodeName)).WithMesh(litMeshRef));
                }

                if (skinned)
                {
                    var container = b.AddNode(GltfContract.SkeletonContainer(nodeName)).AsRoot();
                    var boneNodes = GltfMapping.AddSkeleton(b, skeleton, warnings, container);

                    GltfMapping.AddDummies(b, skeleton, boneNodes, warnings);

                    node.WithSkin(AddSkin(b, skeleton, boneNodes, nodeName));
                    skinnedObjects.Add((obj, skeleton, boneNodes));
                }
            }

            var skinnedObjectIds = new HashSet<uint>();

            foreach (var entry in skinnedObjects)
            {
                skinnedObjectIds.Add(entry.Object.Id);
            }

            foreach (var obj in objects)
            {
                var node = nodeByObjectId[obj.Id];

                if (obj.ParentId != uint.MaxValue && nodeByObjectId.TryGetValue(obj.ParentId, out var parent))
                {
                    // glTF ignores a skinned node's ancestor chain just as it
                    // ignores its own transform: the mesh follows its bones.
                    if (skinnedObjectIds.Contains(obj.Id))
                    {
                        warnings.Add($"object {obj.Id} is skinned; its parent {obj.ParentId} is ignored");
                    }

                    parent.AddChild(node);
                }
                else
                {
                    node.AsRoot();
                }
            }

            AddHelperNodes(b, helpers);
            AddAnimations(b, objects, nodeByObjectId, skinnedObjectIds, warnings);
            AddBoneAnimations(b, skinnedObjects, warnings);

            return Finish(b, warnings);
        }

        internal static GltfConversion Finish(GltfBuilder b, List<string> warnings)
        {
            var file = b.Build();

            return new GltfConversion
            {
                Document = file.Document,
                Bin = file.BinChunk,
                Warnings = warnings,
            };
        }

        private static void AddHelperNodes(GltfBuilder b, Helper[] helpers)
        {
            var helperIndex = 0;

            foreach (var helper in helpers)
            {
                if (helper.Dummies != null)
                {
                    foreach (var dummy in helper.Dummies)
                    {
                        var dummyNode = b.AddNode(GltfContract.Dummy(dummy.Id));

                        if (!dummy.Matrix.IsIdentity)
                        {
                            dummyNode.WithMatrix(
                                GltfMapping.ToGltfMatrix(GltfMapping.ToGltf(dummy.Matrix)));
                        }

                        dummyNode.AsRoot();
                    }
                }

                if (helper.Meshes == null)
                {
                    continue;
                }

                foreach (var helperMesh in helper.Meshes)
                {
                    var index = helperIndex++;
                    var nodeName = GltfContract.Helper(helperMesh.Name, index);
                    var node = b.AddNode(nodeName).WithMesh(AddHelperMesh(b, helperMesh, nodeName));

                    if (!helperMesh.Matrix.IsIdentity)
                    {
                        node.WithMatrix(
                            GltfMapping.ToGltfMatrix(GltfMapping.ToGltf(helperMesh.Matrix)));
                    }

                    node.AsRoot();
                }
            }
        }

        internal static (int? Joints, int? Weights) AddSkinAttributes(BufferBuilder builder, Mesh mesh)
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

            return (builder.AddJoints(joints), builder.AddVec4(weights, vertexData: true));
        }

        internal static GltfSkinRef AddSkin(GltfBuilder b, BoneAnimation skeleton,
            GltfNodeRef[] boneNodes, string name)
        {
            var inverseBind = new Matrix4x4[skeleton.Bones.Length];

            for (var i = 0; i < inverseBind.Length; i++)
            {
                inverseBind[i] = GltfMapping.ToGltf(skeleton.Bones[i].InvBindMatrix);
            }

            var skin = b.AddSkin(name, boneNodes, b.Buffer.AddMatrices(inverseBind));
            var root = Array.FindIndex(skeleton.Bones, bone => bone.ParentId < 0);

            if (root >= 0)
            {
                skin.WithSkeletonRoot(boneNodes[root]);
            }

            return skin;
        }

        internal static bool HasBlendData(Mesh mesh)
        {
            return mesh.SkinBlends != null && mesh.SkinBlends.Length > 0
                && mesh.BoneIndices != null && mesh.BoneIndices.Length > 0;
        }

        internal static (GltfMeshRef Mesh, GltfMeshRef LitMesh) AddGeometryMesh(GltfBuilder b,
            GeometryObject obj, string name, string modelName, string textureUriPrefix,
            int? litSubset, List<string> warnings,
            int? jointsAccessor = null, int? weightsAccessor = null)
        {
            var mesh = obj.Mesh;
            var positions = new Vector3[mesh.Vertices.Length];

            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = GltfMapping.ToGltf(mesh.Vertices[i]);
            }

            var positionAccessor = b.Buffer.AddVec3(positions, withMinMax: true);

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
                        // Source space is Z-up, so the fallback points up.
                        normal = Vector3.UnitZ;
                        degenerate++;
                    }
                    else
                    {
                        normal /= MathF.Sqrt(lengthSquared);
                    }

                    normals[i] = GltfMapping.ToGltf(normal);
                }

                if (degenerate > 0)
                {
                    warnings.Add($"object {obj.Id} has {degenerate} degenerate normals; replaced");
                }

                normalAccessor = b.Buffer.AddVec3(normals, withMinMax: false);
            }

            int? uvAccessor = null;

            if (mesh.TextureCoordinates != null && mesh.TextureCoordinates.Length > 0 &&
                mesh.TextureCoordinates[0] != null)
            {
                uvAccessor = b.Buffer.AddVec2(mesh.TextureCoordinates[0]);
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

                colorAccessor = b.Buffer.AddColors(rgba, mesh.VertexColors.Length);
            }

            var gltfMesh = b.AddMesh(name);
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

                var attributes = new Dictionary<string, int> { ["POSITION"] = positionAccessor };

                if (normalAccessor != null)
                {
                    attributes["NORMAL"] = normalAccessor.Value;
                }

                if (uvAccessor != null)
                {
                    attributes["TEXCOORD_0"] = uvAccessor.Value;
                }

                if (colorAccessor != null)
                {
                    attributes["COLOR_0"] = colorAccessor.Value;
                }

                if (jointsAccessor != null && weightsAccessor != null)
                {
                    attributes["JOINTS_0"] = jointsAccessor.Value;
                    attributes["WEIGHTS_0"] = weightsAccessor.Value;
                }

                GltfMaterialRef material = null;

                if (textureUriPrefix != null && obj.Materials != null && subsetIndex < obj.Materials.Length)
                {
                    material = AddPreviewMaterial(b, obj, subsetIndex,
                        GltfContract.Material(modelName, obj.Id, subsetIndex), textureUriPrefix,
                        warnings);
                }

                var indicesAccessor = b.Buffer.AddIndices(indices);

                if (subsetIndex == litSubset && mesh.Subsets.Length > 1)
                {
                    litMesh ??= b.AddMesh(GltfContract.LitShell(name));
                    litMesh.AddPrimitive(attributes, indicesAccessor, material);
                }
                else
                {
                    gltfMesh.AddPrimitive(attributes, indicesAccessor, material);
                }
            }

            return (gltfMesh, litMesh);
        }

        private static GltfMaterialRef AddPreviewMaterial(GltfBuilder b,
            GeometryObject obj, int materialIndex, string materialName, string textureUriPrefix,
            List<string> warnings)
        {
            var state = RenderStateResolver.Resolve(obj, materialIndex);
            var fileName = state.TextureFile;
            // Opacity blends the same way the engine does it, so it decides
            // the alpha mode alongside the blend state.
            var blend = state.BlendEnabled || state.OpacityDriven;
            var material = b.AddMaterial(materialName)
                .WithDoubleSided(state.Cull == D3DCull.None);

            foreach (var warning in state.Warnings)
            {
                warnings.Add($"{materialName}: {warning}");
            }

            if (state.Opacity != 1f)
            {
                material.WithBaseColorFactor(new[] { 1f, 1f, 1f, state.Opacity });
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                material.WithTexture(b.AddTexture(
                    $"{textureUriPrefix}/{Path.GetFileNameWithoutExtension(fileName)}.png"));
            }

            if (blend)
            {
                material.WithAlphaBlend();
            }
            else if (state.AlphaTest)
            {
                material.WithAlphaMask(state.Cutoff);
            }

            return material;
        }

        private static GltfMeshRef AddHelperMesh(GltfBuilder b, HelperMesh helperMesh, string name)
        {
            var positions = new Vector3[helperMesh.Vertices.Length];

            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = GltfMapping.ToGltf(helperMesh.Vertices[i]);
            }

            var indices = new uint[helperMesh.Faces.Length * 3];

            for (var f = 0; f < helperMesh.Faces.Length; f++)
            {
                indices[(f * 3) + 0] = helperMesh.Faces[f].Vertex[0];
                indices[(f * 3) + 1] = helperMesh.Faces[f].Vertex[2];
                indices[(f * 3) + 2] = helperMesh.Faces[f].Vertex[1];
            }

            return b.AddMesh(name).AddPrimitive(
                new Dictionary<string, int>
                {
                    ["POSITION"] = b.Buffer.AddVec3(positions, withMinMax: true),
                },
                b.Buffer.AddIndices(indices));
        }

        private static void AddAnimations(GltfBuilder b, GeometryObject[] objects,
            Dictionary<uint, GltfNodeRef> nodeByObjectId,
            HashSet<uint> skinnedObjectIds, List<string> warnings)
        {
            foreach (var obj in objects)
            {
                var frames = obj.Animation?.Matrix?.Frames;

                if (frames == null || frames.Length == 0)
                {
                    continue;
                }

                // Bone animation already claimed this object's clip name
                // (geom_<id>) and the warning for the collision; see the
                // main loop.
                if (skinnedObjectIds.Contains(obj.Id))
                {
                    continue;
                }

                var translations = new Vector3[frames.Length];
                var rotations = new Vector4[frames.Length];
                var scales = new Vector3[frames.Length];
                var previous = Quaternion.Identity;
                var decomposed = true;

                for (var i = 0; i < frames.Length; i++)
                {
                    if (!Matrix4x4.Decompose(GltfMapping.ToGltf(frames[i]),
                            out var scale, out var rotation, out var translation))
                    {
                        decomposed = false;
                        break;
                    }

                    if (i > 0 && Quaternion.Dot(previous, rotation) < 0)
                    {
                        rotation = new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, -rotation.W);
                    }

                    previous = rotation;
                    translations[i] = translation;
                    rotations[i] = new Vector4(rotation.X, rotation.Y, rotation.Z, rotation.W);
                    scales[i] = scale;
                }

                if (!decomposed)
                {
                    warnings.Add($"matrix animation on object {obj.Id} is not TRS-decomposable; skipped");
                    continue;
                }

                var times = new float[frames.Length];

                for (var i = 0; i < frames.Length; i++)
                {
                    times[i] = i / GltfContract.AnimationFramesPerSecond;
                }

                var node = nodeByObjectId[obj.Id];
                var input = b.Buffer.AddScalars(times, withMinMax: true);

                b.AddAnimation(node.Name)
                    .AddChannel(input,
                        b.Buffer.AddVec3(translations, withMinMax: false, vertexData: false),
                        node, "translation")
                    .AddChannel(input, b.Buffer.AddVec4(rotations), node, "rotation")
                    .AddChannel(input,
                        b.Buffer.AddVec3(scales, withMinMax: false, vertexData: false),
                        node, "scale");
            }
        }

        private static void AddBoneAnimations(GltfBuilder b,
            List<(GeometryObject Object, BoneAnimation Skeleton, GltfNodeRef[] BoneNodes)> skinnedObjects,
            List<string> warnings)
        {
            foreach (var entry in skinnedObjects)
            {
                var skeleton = entry.Skeleton;
                var tracks = BoneTracks.Convert(skeleton, warnings);

                BoneTracks.AddAnimation(b, skeleton, entry.BoneNodes, tracks,
                    0, skeleton.FrameCount - 1, GltfContract.Geometry(entry.Object.Id), warnings);
            }
        }
    }
}
