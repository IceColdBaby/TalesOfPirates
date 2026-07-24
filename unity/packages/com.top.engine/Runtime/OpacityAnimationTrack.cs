using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// A keyed material-opacity track (parallel key-frame and value
    /// arrays), referenced by prefab components so prefabs stay
    /// structural.
    /// </summary>
    public sealed class OpacityAnimationTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public int[] keyFrames;
        public float[] values;
    }
}
