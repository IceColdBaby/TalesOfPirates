using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Plays an OpacityAnimationTrack on one material slot. Values lerp
    /// linearly between keys and the track loops one frame past its last
    /// key, matching the original engine's float key sets. Sampling uses
    /// global time, so visibility culling pauses cost nothing and lose
    /// no phase.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class OpacityAnimation : MonoBehaviour
    {
        private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");

        public int materialIndex;
        public OpacityAnimationTrack track;

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
            if (track == null || track.keyFrames == null || track.keyFrames.Length == 0
                || track.values == null || track.values.Length != track.keyFrames.Length)
            {
                return;
            }

            var frame = LoopFrame(track, Time.time * track.framesPerSecond);

            _renderer.GetPropertyBlock(_block, materialIndex);
            _block.SetFloat(OpacityProperty, Sample(track, frame));
            _renderer.SetPropertyBlock(_block, materialIndex);
        }

        public static float LoopFrame(OpacityAnimationTrack track, float frame)
        {
            return frame % (track.keyFrames[track.keyFrames.Length - 1] + 1);
        }

        public static float Sample(OpacityAnimationTrack track, float frame)
        {
            var keys = track.keyFrames;
            var values = track.values;

            if (frame <= keys[0])
            {
                return values[0];
            }

            for (var i = 1; i < keys.Length; i++)
            {
                // A duplicate adjacent key cannot divide by zero here:
                // entering the body needs frame > keys[i - 1] and
                // frame <= keys[i], impossible when the two are equal.
                if (frame <= keys[i])
                {
                    var t = (frame - keys[i - 1]) / (keys[i] - keys[i - 1]);

                    return Mathf.Lerp(values[i - 1], values[i], t);
                }
            }

            return values[values.Length - 1];
        }
    }
}
