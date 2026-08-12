using UnityEngine;

namespace Top.Client.Assets.Models
{
    /// <summary>
    /// A keyed material-opacity track (parallel key-frame and value arrays).
    /// </summary>
    public class OpacityAnimationTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public int[] keyFrames;
        public float[] values;
    }
}
