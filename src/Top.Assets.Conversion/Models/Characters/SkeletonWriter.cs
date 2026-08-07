using System;
using System.Numerics;
using Top.Assets.Contract.Models;
using Top.Assets.Conversion.Models.Gltf;
using Top.Gltf;
using Top.Logging;
using Top.MindPower.Animation;
using Top.Tables.Custom;

namespace Top.Assets.Conversion.Models.Characters
{
    /// <summary>
    /// Writes a skeleton and everything that hangs off it into a glTF document:
    /// the bone nodes, the dummies parented to them, the skin a mesh binds, and
    /// the clips cut out of the timeline.
    /// </summary>
    public class SkeletonWriter
    {
        private readonly GltfBuilder _gltf;

        public SkeletonWriter(GltfBuilder gltf)
        {
            _gltf = gltf;
        }

        /// <summary>
        /// One node per bone, parented to each other. Bones with no parent hang
        /// off <paramref name="parent"/>, or become roots when it is absent.
        /// </summary>
        public GltfNodeRef[] AddSkeleton(BoneAnimation skeleton, GltfNodeRef parent = null)
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
                var node = _gltf.AddNode(string.IsNullOrEmpty(bone.Name) ? $"bone_{i}" : bone.Name);
                var local = bone.ParentId >= 0 && bone.ParentId < skeleton.Bones.Length
                    ? bindWorld[i] * skeleton.Bones[bone.ParentId].InvBindMatrix
                    : bindWorld[i];

                if (!local.IsIdentity)
                {
                    if (Matrix4x4.Decompose(Axis.ToGltf(local),
                            out var scale, out var rotation, out var translation))
                    {
                        node.WithTrs(translation, rotation, scale);
                    }
                    else
                    {
                        Log.Warning($"bind pose of bone {node.Name} is not TRS-decomposable, kept as a matrix node");
                        node.WithMatrix(Axis.ToGltfMatrix(Axis.ToGltf(local)));
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

        public void AddDummies(BoneAnimation skeleton, GltfNodeRef[] boneNodes)
        {
            if (skeleton.Dummies == null)
            {
                return;
            }

            foreach (var dummy in skeleton.Dummies)
            {
                var node = _gltf.AddNode(Naming.Dummy(dummy.Id));
                var parented = dummy.ParentBoneId < boneNodes.Length;

                var local = parented
                    ? dummy.Matrix * skeleton.Bones[dummy.ParentBoneId].InvBindMatrix
                    : dummy.Matrix;

                Axis.Place(node, local);

                if (parented)
                {
                    boneNodes[dummy.ParentBoneId].AddChild(node);
                }
                else
                {
                    Log.Warning($"dummy {dummy.Id} references missing bone {dummy.ParentBoneId}");
                    node.AsRoot();
                }
            }
        }

        public GltfSkinRef AddSkin(BoneAnimation skeleton, GltfNodeRef[] boneNodes, string name)
        {
            var inverseBind = new Matrix4x4[skeleton.Bones.Length];

            for (var i = 0; i < inverseBind.Length; i++)
            {
                inverseBind[i] = Axis.ToGltf(skeleton.Bones[i].InvBindMatrix);
            }

            var skin = _gltf.AddSkin(name, boneNodes, _gltf.Buffer.AddMatrices(inverseBind));
            var root = Array.FindIndex(skeleton.Bones, bone => bone.ParentId < 0);

            if (root >= 0)
            {
                skin.WithSkeletonRoot(boneNodes[root]);
            }

            return skin;
        }

        /// <summary>
        /// One clip per action. Without an action table the whole timeline
        /// becomes a single clip.
        /// </summary>
        public void AddClips(BoneAnimation skeleton, GltfNodeRef[] boneNodes, CharacterAction[] actions,
            string modelName)
        {
            if (actions == null || actions.Length == 0)
            {
                actions = new[]
                {
                    new CharacterAction { StartFrame = 0, EndFrame = skeleton.FrameCount - 1 },
                };

                Log.Warning("no action table entry, emitting the full timeline as one clip");
            }

            var tracks = BoneTracks.Convert(skeleton);

            foreach (var action in actions)
            {
                AddBoneAnimation(skeleton, boneNodes, tracks, action.StartFrame, action.EndFrame,
                    Naming.ActionClip(modelName, action.ActionNo));
            }
        }

        public void AddBoneAnimation(BoneAnimation skeleton, GltfNodeRef[] boneNodes, TrackSamples[] tracks,
            int startFrame, int endFrame, string name)
        {
            var start = startFrame;
            var end = endFrame;

            if (start < 0 || end >= skeleton.FrameCount)
            {
                Log.Warning($"clip {name} range {start}..{end} clamped to 0..{skeleton.FrameCount - 1}");
                start = Math.Max(start, 0);
                end = Math.Min(end, skeleton.FrameCount - 1);
            }

            if (end < start)
            {
                Log.Warning($"clip {name} has an empty range, skipped");

                return;
            }

            var length = end - start + 1;
            var animation = _gltf.AddAnimation(name);
            var input = _gltf.Buffer.AddScalars(Timeline.Seconds(length), withMinMax: true);

            for (var i = 0; i < boneNodes.Length; i++)
            {
                var track = tracks[i];

                if (track == null)
                {
                    continue;
                }

                // glTF forbids TRS channels on a node that kept a matrix.
                if (boneNodes[i].HasMatrix)
                {
                    continue;
                }

                if (start + length > track.Positions.Length)
                {
                    Log.Warning($"clip {name}: track of bone {skeleton.Bones[i].Name} " +
                                "is shorter than the timeline, the bone stays in bind pose");
                    continue;
                }

                var translations = new Vector3[length];
                var rotations = new Vector4[length];

                Array.Copy(track.Positions, start, translations, 0, length);
                Array.Copy(track.Rotations, start, rotations, 0, length);

                animation
                    .AddChannel(input,
                        _gltf.Buffer.AddVec3(translations, withMinMax: false, vertexData: false),
                        boneNodes[i], "translation")
                    .AddChannel(input, _gltf.Buffer.AddVec4(rotations), boneNodes[i], "rotation");
            }

            if (animation.ChannelCount == 0)
            {
                // The builder drops it, glTF rejects a channelless animation.
                Log.Warning($"clip {name} has no usable tracks, skipped");
            }
        }
    }
}
