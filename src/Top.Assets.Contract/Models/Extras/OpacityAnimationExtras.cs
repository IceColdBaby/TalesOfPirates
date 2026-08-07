using System.ComponentModel;
using Newtonsoft.Json;

namespace Top.Assets.Contract.Models.Extras
{
    /// <summary>
    /// Sparse opacity keys, <see cref="KeyFrames"/> and <see cref="Values"/>
    /// running in parallel. Values hold before the first key and after the
    /// last, ease linearly between neighbours, and the track loops one frame
    /// past its last key.
    /// </summary>
    public class OpacityAnimationExtras
    {
        [JsonProperty("framesPerSecond")]
        [DefaultValue(AnimationRate.FramesPerSecond)]
        public float FramesPerSecond = AnimationRate.FramesPerSecond;

        [JsonProperty("keyFrames")] public int[] KeyFrames;
        [JsonProperty("values")] public float[] Values;
    }
}
