using System.ComponentModel;
using Newtonsoft.Json;

namespace Top.Contracts.Assets.Models.Extras
{
    /// <summary>
    /// A per-frame texture transform. Frames play at
    /// <see cref="FramesPerSecond"/> and loop on the last one.
    /// </summary>
    public class UvAnimationExtras
    {
        [JsonProperty("framesPerSecond")] [DefaultValue(AnimationRate.FramesPerSecond)]
        public float FramesPerSecond = AnimationRate.FramesPerSecond;

        /// <summary>
        /// Six values per frame, the transform applied as [u v 1] * M taken
        /// row by row: m11 m12 m21 m22 m31 m32. The cells left out never
        /// reach a texture coordinate.
        /// </summary>
        [JsonProperty("frames")] public float[][] Frames;
    }
}
