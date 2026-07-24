using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// A legacy per-frame texture-transform track (D3D COUNT2 semantics),
    /// referenced by prefab components so prefabs stay structural.
    /// </summary>
    public sealed class UvAnimationTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public Matrix4x4[] frames;
    }
}
