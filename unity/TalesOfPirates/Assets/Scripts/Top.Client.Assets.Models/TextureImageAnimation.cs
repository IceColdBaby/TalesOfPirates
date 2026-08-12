using UnityEngine;

namespace Top.Client.Assets.Models
{
    /// <summary>
    /// Plays a TextureImageTrack on one material slot.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class TextureImageAnimation : MonoBehaviour
    {
        private static readonly int BaseMapProperty = Shader.PropertyToID("_BaseMap");

        public int materialIndex;
        public TextureImageTrack track;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void OnBecameVisible()
        {
            enabled = true;
        }

        private void OnBecameInvisible()
        {
            enabled = false;
        }

        private void Update()
        {
            if (track == null || track.frames == null || track.frames.Length == 0)
            {
                return;
            }

            var index = FrameIndex(Time.time, track.framesPerSecond, track.frames.Length);
            var frame = track.frames[index];

            if (frame == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(_block, materialIndex);
            _block.SetTexture(BaseMapProperty, frame);
            _renderer.SetPropertyBlock(_block, materialIndex);
        }

        public static int FrameIndex(float time, float framesPerSecond, int frameCount)
        {
            return (int)(time * framesPerSecond) % frameCount;
        }
    }
}
