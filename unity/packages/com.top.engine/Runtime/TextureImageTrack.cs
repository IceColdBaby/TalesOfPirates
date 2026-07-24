using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// A legacy per-frame texture flipbook track; each animation frame selects
    /// one texture, referenced by prefab components so prefabs stay structural.
    /// </summary>
    public sealed class TextureImageTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public Texture2D[] frames;
    }
}
