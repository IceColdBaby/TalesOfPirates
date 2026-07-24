using System.Collections.Generic;
using System.IO;
using Top.Assets.Conversion;
using Top.Gltf;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Top.Engine.Editor
{
    /// <summary>
    /// Geometry-only glTF importer: Mesh sub-assets named by glTF mesh name
    /// plus a preview hierarchy. Material definitions in the file are never
    /// imported: engine semantics live in Unity assets. Renderers bind
    /// project materials remapped by glTF material name via the external
    /// object map, falling back to a preview material. Animations become
    /// looping legacy clips hosted on the deepest node that contains all of
    /// their targets, with curve paths relative to that node. Skinned
    /// meshes import as SkinnedMeshRenderers bound to the node hierarchy
    /// declared by their glTF skin. A host with no renderer beneath it
    /// always animates; one that carries a renderer culls with it.
    /// </summary>
    [ScriptedImporter(1, new[] { "glb", "gltf" })]
    public sealed class GltfImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            GltfFile file;
            using (var stream = File.OpenRead(ctx.assetPath))
            {
                file = GltfReader.Read(stream);
            }

            var directory = Path.GetDirectoryName(ctx.assetPath);
            var data = new GltfData(file, uri =>
            {
                var path = Path.Combine(directory, uri);
                ctx.DependsOnSourceAsset(path);
                return File.ReadAllBytes(path);
            });

            var doc = file.Document;
            var meshes = new Mesh[doc.Meshes?.Count ?? 0];
            var usedIds = new HashSet<string>();
            for (var i = 0; i < meshes.Length; i++)
            {
                meshes[i] = BuildMesh(data, i);
                var id = meshes[i].name;
                while (!usedIds.Add(id))
                {
                    id += "_";
                }

                ctx.AddObjectToAsset(id, meshes[i]);
            }

            var root = new GameObject(Path.GetFileNameWithoutExtension(ctx.assetPath));
            if (doc.Nodes != null)
            {
                var externalObjects = GetExternalObjectMap();
                var objects = new GameObject[doc.Nodes.Count];
                for (var i = 0; i < doc.Nodes.Count; i++)
                {
                    var node = doc.Nodes[i];
                    objects[i] = new GameObject(node.Name ?? $"node_{i}");
                    ApplyTransform(objects[i].transform, node);
                    if (node.Mesh != null && node.Skin == null)
                    {
                        objects[i].AddComponent<MeshFilter>().sharedMesh = meshes[node.Mesh.Value];
                        if (!GltfContract.IsHelper(objects[i].name))
                        {
                            objects[i].AddComponent<MeshRenderer>().sharedMaterials =
                                ResolveMaterials(doc, node.Mesh.Value, externalObjects);
                        }
                    }
                }

                for (var i = 0; i < doc.Nodes.Count; i++)
                {
                    if (doc.Nodes[i].Children == null)
                    {
                        continue;
                    }

                    foreach (var child in doc.Nodes[i].Children)
                    {
                        objects[child].transform.SetParent(
                            objects[i].transform, worldPositionStays: false);
                    }
                }

                foreach (var go in objects)
                {
                    if (go.transform.parent == null)
                    {
                        go.transform.SetParent(root.transform, worldPositionStays: false);
                    }
                }

                for (var i = 0; i < doc.Nodes.Count; i++)
                {
                    var node = doc.Nodes[i];
                    if (node.Mesh == null || node.Skin == null)
                    {
                        continue;
                    }

                    var skin = doc.Skins[node.Skin.Value];
                    var mesh = meshes[node.Mesh.Value];
                    var bones = new Transform[skin.Joints.Count];
                    for (var j = 0; j < bones.Length; j++)
                    {
                        bones[j] = objects[skin.Joints[j]].transform;
                    }

                    if (skin.InverseBindMatrices != null)
                    {
                        var ibm = data.ReadFloats(skin.InverseBindMatrices.Value);
                        var bindposes = new Matrix4x4[bones.Length];
                        for (var j = 0; j < bindposes.Length; j++)
                        {
                            bindposes[j] = ToUnityMatrix(ibm, j * 16);
                        }

                        mesh.bindposes = bindposes;
                    }

                    var renderer = objects[i].AddComponent<SkinnedMeshRenderer>();
                    renderer.bones = bones;
                    renderer.rootBone = skin.Skeleton != null
                        ? objects[skin.Skeleton.Value].transform
                        : bones[0];
                    renderer.sharedMesh = mesh;
                    renderer.updateWhenOffscreen = true;
                    renderer.sharedMaterials = ResolveMaterials(doc, node.Mesh.Value, externalObjects);
                }

                if (doc.Animations != null)
                {
                    var parents = BuildParents(doc);
                    for (var i = 0; i < doc.Animations.Count; i++)
                    {
                        var hostNode = HostNode(doc.Animations[i], parents);
                        var host = hostNode != null ? objects[hostNode.Value] : root;
                        var clip = BuildClip(data, doc.Animations[i], i, host.transform, objects);
                        ctx.AddObjectToAsset("anim_" + clip.name, clip);

                        var animation = host.GetComponent<Animation>();
                        if (animation == null)
                        {
                            animation = host.AddComponent<Animation>();
                            animation.playAutomatically = true;
                            // BasedOnRenderers looks for Renderers under the
                            // Animation's own hierarchy; a bone clip hosted
                            // on a skeleton root has none there (the
                            // SkinnedMeshRenderer is a sibling), so that
                            // culling mode would never sample the clip.
                            animation.cullingType = host.GetComponentInChildren<Renderer>(true) != null
                                ? AnimationCullingType.BasedOnRenderers
                                : AnimationCullingType.AlwaysAnimate;
                        }

                        if (animation.clip == null)
                        {
                            animation.clip = clip;
                        }

                        animation.AddClip(clip, clip.name);
                    }
                }
            }

            ctx.AddObjectToAsset("root", root);
            ctx.SetMainObject(root);
        }

        private static Mesh BuildMesh(GltfData data, int meshIndex)
        {
            var gltfMesh = data.Document.Meshes[meshIndex];
            var mesh = new Mesh { name = gltfMesh.Name ?? $"mesh_{meshIndex}" };
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var boneWeights = new List<BoneWeight>();
            var submeshes = new List<int[]>();

            foreach (var primitive in gltfMesh.Primitives)
            {
                var vertexStart = positions.Count;
                var p = data.ReadFloats(primitive.Attributes["POSITION"]);
                for (var i = 0; i < p.Length; i += 3)
                {
                    positions.Add(new Vector3(p[i], p[i + 1], -p[i + 2]));
                }

                if (primitive.Attributes.TryGetValue("NORMAL", out var normalAccessor))
                {
                    var n = data.ReadFloats(normalAccessor);
                    for (var i = 0; i < n.Length; i += 3)
                    {
                        normals.Add(new Vector3(n[i], n[i + 1], -n[i + 2]));
                    }
                }

                if (primitive.Attributes.TryGetValue("TEXCOORD_0", out var uvAccessor))
                {
                    var uv = data.ReadFloats(uvAccessor);
                    for (var i = 0; i < uv.Length; i += 2)
                    {
                        uvs.Add(new Vector2(uv[i], 1f - uv[i + 1]));
                    }
                }

                if (primitive.Attributes.TryGetValue("COLOR_0", out var colorAccessor))
                {
                    var c = data.ReadFloats(colorAccessor);
                    var components = GltfData.ComponentCount(
                        data.Document.Accessors[colorAccessor].Type);
                    for (var i = 0; i < c.Length; i += components)
                    {
                        colors.Add(new Color(c[i], c[i + 1], c[i + 2],
                            components == 4 ? c[i + 3] : 1f));
                    }
                }

                if (primitive.Attributes.TryGetValue("JOINTS_0", out var jointsAccessor)
                    && primitive.Attributes.TryGetValue("WEIGHTS_0", out var weightsAccessor))
                {
                    var joints = data.ReadInts(jointsAccessor);
                    var weights = data.ReadFloats(weightsAccessor);
                    for (var i = 0; i < weights.Length; i += 4)
                    {
                        boneWeights.Add(new BoneWeight
                        {
                            boneIndex0 = joints[i],
                            boneIndex1 = joints[i + 1],
                            boneIndex2 = joints[i + 2],
                            boneIndex3 = joints[i + 3],
                            weight0 = weights[i],
                            weight1 = weights[i + 1],
                            weight2 = weights[i + 2],
                            weight3 = weights[i + 3],
                        });
                    }
                }

                if (primitive.Indices != null)
                {
                    var indices = data.ReadInts(primitive.Indices.Value);
                    for (var t = 0; t < indices.Length; t += 3)
                    {
                        (indices[t + 1], indices[t + 2]) = (indices[t + 2], indices[t + 1]);
                    }

                    for (var i = 0; i < indices.Length; i++)
                    {
                        indices[i] += vertexStart;
                    }

                    submeshes.Add(indices);
                }
            }

            if (positions.Count > ushort.MaxValue)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(positions);
            if (normals.Count == positions.Count)
            {
                mesh.SetNormals(normals);
            }

            if (uvs.Count == positions.Count)
            {
                mesh.SetUVs(0, uvs);
            }

            if (colors.Count == positions.Count)
            {
                mesh.SetColors(colors);
            }

            if (boneWeights.Count == positions.Count)
            {
                mesh.boneWeights = boneWeights.ToArray();
            }

            mesh.subMeshCount = submeshes.Count;
            for (var s = 0; s < submeshes.Count; s++)
            {
                mesh.SetIndices(submeshes[s], MeshTopology.Triangles, s);
            }

            if (normals.Count != positions.Count)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private static Matrix4x4 ToUnityMatrix(float[] m, int offset)
        {
            var u = new Matrix4x4(
                new Vector4(m[offset], m[offset + 1], m[offset + 2], m[offset + 3]),
                new Vector4(m[offset + 4], m[offset + 5], m[offset + 6], m[offset + 7]),
                new Vector4(m[offset + 8], m[offset + 9], m[offset + 10], m[offset + 11]),
                new Vector4(m[offset + 12], m[offset + 13], m[offset + 14], m[offset + 15]));
            // F * M * F with F = diag(1, 1, -1, 1).
            u.m02 = -u.m02;
            u.m12 = -u.m12;
            u.m32 = -u.m32;
            u.m20 = -u.m20;
            u.m21 = -u.m21;
            u.m23 = -u.m23;
            return u;
        }

        private static bool IsOrthogonal(Matrix4x4 u)
        {
            Vector3 right = u.GetColumn(0);
            Vector3 up = u.GetColumn(1);
            Vector3 forward = u.GetColumn(2);
            right.Normalize();
            up.Normalize();
            forward.Normalize();

            return Mathf.Abs(Vector3.Dot(right, up)) < 1e-3f
                && Mathf.Abs(Vector3.Dot(right, forward)) < 1e-3f
                && Mathf.Abs(Vector3.Dot(up, forward)) < 1e-3f;
        }

        private static void ApplyTransform(Transform transform, GltfNode node)
        {
            if (node.Matrix != null)
            {
                var u = ToUnityMatrix(node.Matrix, 0);
                transform.localPosition = u.GetColumn(3);

                if (u.ValidTRS() && IsOrthogonal(u))
                {
                    transform.localRotation = u.rotation;
                    transform.localScale = u.lossyScale;
                }
                else
                {
                    // Sheared bind poses (the conversion-side matrix
                    // fallback) pass ValidTRS, which only rejects non-affine
                    // rows, yet a Transform cannot represent them. Decompose
                    // best effort: axis lengths as scale, orthonormalized
                    // rotation.
                    Vector3 right = u.GetColumn(0);
                    Vector3 up = u.GetColumn(1);
                    Vector3 forward = u.GetColumn(2);
                    transform.localRotation = Quaternion.LookRotation(forward, up);
                    transform.localScale = new Vector3(
                        right.magnitude, up.magnitude, forward.magnitude);
                }

                return;
            }

            if (node.Translation != null)
            {
                transform.localPosition = new Vector3(
                    node.Translation[0], node.Translation[1], -node.Translation[2]);
            }

            if (node.Rotation != null)
            {
                transform.localRotation = new Quaternion(
                    -node.Rotation[0], -node.Rotation[1], node.Rotation[2], node.Rotation[3]);
            }

            if (node.Scale != null)
            {
                transform.localScale = new Vector3(
                    node.Scale[0], node.Scale[1], node.Scale[2]);
            }
        }

        private static Material[] ResolveMaterials(GltfDocument doc, int meshIndex,
            Dictionary<SourceAssetIdentifier, UnityEngine.Object> externalObjects)
        {
            var primitives = doc.Meshes[meshIndex].Primitives;
            var preview = PreviewMaterial();
            var materials = new Material[primitives.Count];
            for (var i = 0; i < primitives.Count; i++)
            {
                materials[i] = preview;
                var materialIndex = primitives[i].Material;
                if (materialIndex == null)
                {
                    continue;
                }

                var name = doc.Materials[materialIndex.Value].Name
                    ?? $"material_{materialIndex.Value}";
                if (externalObjects.TryGetValue(
                        new SourceAssetIdentifier(typeof(Material), name), out var mapped)
                    && mapped is Material material)
                {
                    materials[i] = material;
                }
            }

            return materials;
        }

        private static Material PreviewMaterial()
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            return pipeline != null
                ? pipeline.defaultMaterial
                : UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>(
                    "Default-Diffuse.mat");
        }

        private static int[] BuildParents(GltfDocument doc)
        {
            var parents = new int[doc.Nodes.Count];
            for (var i = 0; i < parents.Length; i++)
            {
                parents[i] = -1;
            }

            for (var i = 0; i < doc.Nodes.Count; i++)
            {
                if (doc.Nodes[i].Children == null)
                {
                    continue;
                }

                foreach (var child in doc.Nodes[i].Children)
                {
                    parents[child] = i;
                }
            }

            return parents;
        }

        private static int? HostNode(GltfAnimation animation, int[] parents)
        {
            int? host = null;
            foreach (var channel in animation.Channels)
            {
                if (channel.Target.Node == null)
                {
                    continue;
                }

                if (host == null)
                {
                    host = channel.Target.Node.Value;
                    continue;
                }

                var common = CommonAncestor(host.Value, channel.Target.Node.Value, parents);
                if (common < 0)
                {
                    return null;
                }

                host = common;
            }

            return host;
        }

        private static int CommonAncestor(int a, int b, int[] parents)
        {
            var seen = new HashSet<int>();
            for (var node = a; node >= 0; node = parents[node])
            {
                seen.Add(node);
            }

            for (var node = b; node >= 0; node = parents[node])
            {
                if (seen.Contains(node))
                {
                    return node;
                }
            }

            return -1;
        }

        private static string RelativePath(Transform host, Transform node)
        {
            var segments = new List<string>();
            var current = node;
            while (current != null && current != host)
            {
                segments.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", segments);
        }

        private static AnimationClip BuildClip(GltfData data, GltfAnimation animation, int index,
            Transform host, GameObject[] objects)
        {
            var clip = new AnimationClip
            {
                name = animation.Name ?? $"anim_{index}",
                legacy = true,
                wrapMode = WrapMode.Loop,
                frameRate = GltfContract.AnimationFramesPerSecond,
            };

            foreach (var channel in animation.Channels)
            {
                if (channel.Target.Node == null)
                {
                    continue;
                }

                var path = RelativePath(host, objects[channel.Target.Node.Value].transform);
                var sampler = animation.Samplers[channel.Sampler];
                var times = data.ReadFloats(sampler.Input);
                var values = data.ReadFloats(sampler.Output);

                switch (channel.Target.Path)
                {
                    case "translation":
                        SetVectorCurves(clip, path, "localPosition", times, values, negateZ: true);
                        break;
                    case "rotation":
                        SetRotationCurves(clip, path, times, values);
                        break;
                    case "scale":
                        SetVectorCurves(clip, path, "localScale", times, values, negateZ: false);
                        break;
                }
            }

            clip.EnsureQuaternionContinuity();
            return clip;
        }

        private static void SetVectorCurves(AnimationClip clip, string path, string property,
            float[] times, float[] values, bool negateZ)
        {
            var x = new Keyframe[times.Length];
            var y = new Keyframe[times.Length];
            var z = new Keyframe[times.Length];
            for (var i = 0; i < times.Length; i++)
            {
                x[i] = new Keyframe(times[i], values[i * 3]);
                y[i] = new Keyframe(times[i], values[(i * 3) + 1]);
                z[i] = new Keyframe(times[i],
                    negateZ ? -values[(i * 3) + 2] : values[(i * 3) + 2]);
            }

            clip.SetCurve(path, typeof(Transform), property + ".x", new AnimationCurve(x));
            clip.SetCurve(path, typeof(Transform), property + ".y", new AnimationCurve(y));
            clip.SetCurve(path, typeof(Transform), property + ".z", new AnimationCurve(z));
        }

        private static void SetRotationCurves(AnimationClip clip, string path,
            float[] times, float[] values)
        {
            var x = new Keyframe[times.Length];
            var y = new Keyframe[times.Length];
            var z = new Keyframe[times.Length];
            var w = new Keyframe[times.Length];
            for (var i = 0; i < times.Length; i++)
            {
                x[i] = new Keyframe(times[i], -values[i * 4]);
                y[i] = new Keyframe(times[i], -values[(i * 4) + 1]);
                z[i] = new Keyframe(times[i], values[(i * 4) + 2]);
                w[i] = new Keyframe(times[i], values[(i * 4) + 3]);
            }

            clip.SetCurve(path, typeof(Transform), "localRotation.x", new AnimationCurve(x));
            clip.SetCurve(path, typeof(Transform), "localRotation.y", new AnimationCurve(y));
            clip.SetCurve(path, typeof(Transform), "localRotation.z", new AnimationCurve(z));
            clip.SetCurve(path, typeof(Transform), "localRotation.w", new AnimationCurve(w));
        }
    }
}
