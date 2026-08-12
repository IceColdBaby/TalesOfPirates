using UnityEngine;

namespace Top.Client.Assets.Models
{
    /// <summary>
    /// A per-frame texture-transform track.
    /// </summary>
    public class UvAnimationTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public Matrix4x4[] frames;
    }
}
