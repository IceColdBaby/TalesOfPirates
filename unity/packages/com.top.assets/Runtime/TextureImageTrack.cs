using UnityEngine;

namespace Top.Assets
{
    /// <summary>
    /// A legacy per-frame texture flipbook track.
    /// </summary>
    public class TextureImageTrack : ScriptableObject
    {
        public float framesPerSecond = 30f;
        public Texture2D[] frames;
    }
}
