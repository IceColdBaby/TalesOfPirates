using UnityEngine;

namespace Top.Client.Assets.Models.Animations
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
