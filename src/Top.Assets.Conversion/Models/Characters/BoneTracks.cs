using System;
using System.Numerics;
using Top.Assets.Conversion.Models.Gltf;
using Top.Logging;
using Top.MindPower.Animation;

namespace Top.Assets.Conversion.Models.Characters
{
    /// <summary>
    /// One bone's converted animation. A bone without samples stays in bind pose.
    /// </summary>
    public class TrackSamples
    {
        public Vector3[] Positions;
        public Vector4[] Rotations;
    }

    /// <summary>
    /// Converts the bone animation tracks of a skeleton into samples the
    /// builder can lay onto a clip.
    /// </summary>
    public static class BoneTracks
    {
        public static TrackSamples[] Convert(BoneAnimation skeleton)
        {
            var tracks = new TrackSamples[skeleton.Bones.Length];

            for (var b = 0; b < skeleton.Bones.Length; b++)
            {
                var track = skeleton.Tracks != null && b < skeleton.Tracks.Length
                    ? skeleton.Tracks[b]
                    : null;

                switch (track)
                {
                    case QuaternionBoneTrack quat when quat.Positions != null && quat.Positions.Length > 0 &&
                                                       quat.Rotations != null && quat.Rotations.Length > 0:
                        tracks[b] = ConvertQuaternionTrack(quat);
                        break;
                    case MatrixBoneTrack matrix:
                        if (!TryConvertMatrixTrack(matrix, out tracks[b]))
                        {
                            Log.Warning($"track of bone {skeleton.Bones[b].Name} is not TRS-decomposable," +
                                        " the bone stays in bind pose");
                        }

                        break;
                    default:
                        Log.Warning($"bone {skeleton.Bones[b].Name} has no track, it stays in bind pose");
                        break;
                }
            }

            return tracks;
        }

        private static TrackSamples ConvertQuaternionTrack(QuaternionBoneTrack track)
        {
            // Old-format .lab tracks store a single position for non-root bones, that position holds for every frame.
            var positions = new Vector3[track.Rotations.Length];
            var rotations = new Vector4[track.Rotations.Length];
            var previous = Quaternion.Identity;

            for (var i = 0; i < rotations.Length; i++)
            {
                positions[i] = Axis.ToGltf(
                    track.Positions[Math.Min(i, track.Positions.Length - 1)]);

                var q = Axis.ToGltf(track.Rotations[i]);
                q = q.LengthSquared() > 0f ? Quaternion.Normalize(q) : Quaternion.Identity;

                if (i > 0 && Quaternion.Dot(previous, q) < 0)
                {
                    q = new Quaternion(-q.X, -q.Y, -q.Z, -q.W);
                }

                previous = q;
                rotations[i] = new Vector4(q.X, q.Y, q.Z, q.W);
            }

            return new TrackSamples { Positions = positions, Rotations = rotations };
        }

        private static bool TryConvertMatrixTrack(MatrixBoneTrack track, out TrackSamples result)
        {
            var positions = new Vector3[track.Frames.Length];
            var rotations = new Vector4[track.Frames.Length];
            var previous = Quaternion.Identity;

            for (var i = 0; i < track.Frames.Length; i++)
            {
                if (!Matrix4x4.Decompose(Axis.ToGltf(track.Frames[i]),
                        out _, out var rotation, out var translation))
                {
                    result = null;

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

            result = new TrackSamples { Positions = positions, Rotations = rotations };

            return true;
        }
    }
}
