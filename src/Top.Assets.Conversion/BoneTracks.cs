using System;
using System.Collections.Generic;
using System.Numerics;
using Top.Gltf;
using Top.MindPower.Animation;

namespace Top.Assets.Conversion
{
    /// <summary>
    /// Per-bone keyframe conversion shared by the rig and scene-model
    /// mappers. Source tracks become glTF translation and rotation channels
    /// over an inclusive frame range, rebased to start at zero and sampled
    /// at the animation frame rate.
    /// </summary>
    internal static class BoneTracks
    {
        internal static (Vector3[] Positions, Vector4[] Rotations)[] Convert(
            BoneAnimation skeleton, List<string> warnings)
        {
            var tracks = new (Vector3[] Positions, Vector4[] Rotations)[skeleton.Bones.Length];

            for (var b = 0; b < skeleton.Bones.Length; b++)
            {
                var track = skeleton.Tracks != null && b < skeleton.Tracks.Length
                    ? skeleton.Tracks[b]
                    : null;

                switch (track)
                {
                    case QuaternionBoneTrack quat when quat.Positions != null
                        && quat.Positions.Length > 0
                        && quat.Rotations != null && quat.Rotations.Length > 0:
                        tracks[b] = ConvertQuaternionTrack(quat);
                        break;
                    case MatrixBoneTrack matrix:
                        if (!TryConvertMatrixTrack(matrix, out tracks[b]))
                        {
                            warnings.Add($"track of bone {skeleton.Bones[b].Name} is not " +
                                "TRS-decomposable; the bone stays in bind pose");
                        }

                        break;
                    default:
                        warnings.Add($"bone {skeleton.Bones[b].Name} has no track; " +
                            "it stays in bind pose");
                        break;
                }
            }

            return tracks;
        }

        internal static void AddAnimation(GltfBuilder b,
            BoneAnimation skeleton, GltfNodeRef[] boneNodes,
            (Vector3[] Positions, Vector4[] Rotations)[] tracks,
            int startFrame, int endFrame, string name, List<string> warnings)
        {
            var start = startFrame;
            var end = endFrame;

            if (start < 0 || end >= skeleton.FrameCount)
            {
                warnings.Add($"clip {name} range {start}..{end} clamped to " +
                    $"0..{skeleton.FrameCount - 1}");
                start = Math.Max(start, 0);
                end = Math.Min(end, skeleton.FrameCount - 1);
            }

            if (end < start)
            {
                warnings.Add($"clip {name} has an empty range; skipped");

                return;
            }

            var length = end - start + 1;
            var times = new float[length];

            for (var i = 0; i < length; i++)
            {
                times[i] = i / GltfContract.AnimationFramesPerSecond;
            }

            var animation = b.AddAnimation(name);
            var input = b.Buffer.AddScalars(times, withMinMax: true);

            for (var i = 0; i < boneNodes.Length; i++)
            {
                var track = tracks[i];

                if (track.Positions == null)
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
                    warnings.Add($"clip {name}: track of bone {skeleton.Bones[i].Name} " +
                        "is shorter than the timeline; the bone stays in bind pose");
                    continue;
                }

                var translations = new Vector3[length];
                var rotations = new Vector4[length];

                Array.Copy(track.Positions, start, translations, 0, length);
                Array.Copy(track.Rotations, start, rotations, 0, length);

                animation
                    .AddChannel(input,
                        b.Buffer.AddVec3(translations, withMinMax: false, vertexData: false),
                        boneNodes[i], "translation")
                    .AddChannel(input, b.Buffer.AddVec4(rotations), boneNodes[i], "rotation");
            }

            if (animation.ChannelCount == 0)
            {
                // The builder drops it; glTF rejects a channelless animation.
                warnings.Add($"clip {name} has no usable tracks; skipped");
            }
        }

        private static (Vector3[] Positions, Vector4[] Rotations) ConvertQuaternionTrack(
            QuaternionBoneTrack track)
        {
            // Old-format .lab tracks store a single position for non-root
            // bones; that position holds for every frame.
            var positions = new Vector3[track.Rotations.Length];
            var rotations = new Vector4[track.Rotations.Length];
            var previous = Quaternion.Identity;

            for (var i = 0; i < rotations.Length; i++)
            {
                positions[i] = GltfMapping.ToGltf(
                    track.Positions[Math.Min(i, track.Positions.Length - 1)]);

                var q = GltfMapping.ToGltf(track.Rotations[i]);
                q = q.LengthSquared() > 0f ? Quaternion.Normalize(q) : Quaternion.Identity;

                if (i > 0 && Quaternion.Dot(previous, q) < 0)
                {
                    q = new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
                }

                previous = q;
                rotations[i] = new Vector4(q.X, q.Y, q.Z, q.W);
            }

            return (positions, rotations);
        }

        private static bool TryConvertMatrixTrack(MatrixBoneTrack track,
            out (Vector3[] Positions, Vector4[] Rotations) result)
        {
            var positions = new Vector3[track.Frames.Length];
            var rotations = new Vector4[track.Frames.Length];
            var previous = Quaternion.Identity;

            for (var i = 0; i < track.Frames.Length; i++)
            {
                if (!Matrix4x4.Decompose(GltfMapping.ToGltf(track.Frames[i]),
                        out _, out var rotation, out var translation))
                {
                    result = (null, null);

                    return false;
                }

                rotation = Quaternion.Normalize(rotation);

                if (i > 0 && Quaternion.Dot(previous, rotation) < 0)
                {
                    rotation = new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, -rotation.W);
                }

                previous = rotation;
                positions[i] = translation;
                rotations[i] = new Vector4(rotation.X, rotation.Y, rotation.Z, rotation.W);
            }

            result = (positions, rotations);

            return true;
        }
    }
}
