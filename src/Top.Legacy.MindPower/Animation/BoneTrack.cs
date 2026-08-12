using System.Numerics;

namespace Top.Legacy.MindPower.Animation
{
    /// <summary>
    /// Per-bone keyframe track; concrete type matches the file's BoneKeyType.
    /// </summary>
    public abstract class BoneTrack
    {
    }

    /// <summary>
    /// Mat43/Mat44 key types: one transform per frame.
    /// </summary>
    public class MatrixBoneTrack : BoneTrack
    {
        public Matrix4x4[] Frames;
    }

    /// <summary>
    /// Quat key type: position and rotation per frame (Positions may be shorter than the frame count for old-version non-root bones).
    /// </summary>
    public class QuaternionBoneTrack : BoneTrack
    {
        public Vector3[] Positions;
        public Quaternion[] Rotations;
    }
}
