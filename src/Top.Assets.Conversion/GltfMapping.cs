using System.Collections.Generic;
using System.Numerics;
using Top.Gltf;
using Top.MindPower.Animation;

namespace Top.Assets.Conversion
{
    /// <summary>
    /// Pieces shared by the geometry and rig mappers: bind-pose skeleton
    /// emission, skeleton dummies, and the axis conversion (ToP is Z-up
    /// left-handed, glTF is Y-up right-handed; swapping Y and Z maps up to +Y
    /// and flips handedness).
    /// </summary>
    internal static class GltfMapping
    {
        internal const string Generator = "Top.Assets.Conversion";

        internal static GltfNodeRef[] AddSkeleton(GltfBuilder b, BoneAnimation skeleton,
            List<string> warnings, GltfNodeRef parent = null)
        {
            var boneNodes = new GltfNodeRef[skeleton.Bones.Length];
            var bindWorld = new Matrix4x4[skeleton.Bones.Length];

            for (var i = 0; i < skeleton.Bones.Length; i++)
            {
                Matrix4x4.Invert(skeleton.Bones[i].InvBindMatrix, out bindWorld[i]);
            }

            for (var i = 0; i < skeleton.Bones.Length; i++)
            {
                var bone = skeleton.Bones[i];
                var node = b.AddNode(string.IsNullOrEmpty(bone.Name) ? $"bone_{i}" : bone.Name);
                var local = bone.ParentId >= 0 && bone.ParentId < skeleton.Bones.Length
                    ? bindWorld[i] * skeleton.Bones[bone.ParentId].InvBindMatrix
                    : bindWorld[i];

                if (!local.IsIdentity)
                {
                    // Animation channels target bone TRS, and glTF forbids
                    // channels on nodes that define a matrix.
                    if (Matrix4x4.Decompose(ToGltf(local),
                            out var scale, out var rotation, out var translation))
                    {
                        node.WithTrs(translation, rotation, scale);
                    }
                    else
                    {
                        warnings.Add($"bind pose of bone {node.Name} is not TRS-decomposable; " +
                            "kept as a matrix node");
                        node.WithMatrix(ToGltfMatrix(ToGltf(local)));
                    }
                }

                boneNodes[i] = node;
            }

            for (var i = 0; i < skeleton.Bones.Length; i++)
            {
                var parentId = skeleton.Bones[i].ParentId;

                if (parentId >= 0 && parentId < skeleton.Bones.Length)
                {
                    boneNodes[parentId].AddChild(boneNodes[i]);
                }
                else if (parent != null)
                {
                    parent.AddChild(boneNodes[i]);
                }
                else
                {
                    boneNodes[i].AsRoot();
                }
            }

            return boneNodes;
        }

        internal static void AddDummies(GltfBuilder b, BoneAnimation skeleton,
            GltfNodeRef[] boneNodes, List<string> warnings)
        {
            if (skeleton.Dummies == null)
            {
                return;
            }

            foreach (var dummy in skeleton.Dummies)
            {
                var node = b.AddNode(GltfContract.Dummy(dummy.Id));
                var parented = dummy.ParentBoneId < boneNodes.Length;
                // Dummy matrices are model space, like bone bind poses, so
                // attaching one to its bone means removing that bone's bind
                // pose first.
                var local = parented
                    ? dummy.Matrix * skeleton.Bones[dummy.ParentBoneId].InvBindMatrix
                    : dummy.Matrix;

                if (!local.IsIdentity)
                {
                    node.WithMatrix(ToGltfMatrix(ToGltf(local)));
                }

                if (parented)
                {
                    boneNodes[dummy.ParentBoneId].AddChild(node);
                }
                else
                {
                    warnings.Add($"dummy {dummy.Id} references missing bone {dummy.ParentBoneId}");
                    node.AsRoot();
                }
            }
        }

        internal static Vector3 ToGltf(Vector3 v)
        {
            return new Vector3(v.X, v.Z, v.Y);
        }

        internal static Quaternion ToGltf(Quaternion q)
        {
            return new Quaternion(q.X, q.Z, q.Y, -q.W);
        }

        internal static Matrix4x4 ToGltf(Matrix4x4 m)
        {
            return new Matrix4x4(
                m.M11, m.M13, m.M12, m.M14,
                m.M31, m.M33, m.M32, m.M34,
                m.M21, m.M23, m.M22, m.M24,
                m.M41, m.M43, m.M42, m.M44);
        }

        internal static float[] ToGltfMatrix(Matrix4x4 m)
        {
            return new[]
            {
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44,
            };
        }
    }
}
