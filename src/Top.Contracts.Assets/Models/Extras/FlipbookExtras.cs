using System.ComponentModel;
using Newtonsoft.Json;

namespace Top.Contracts.Assets.Models.Extras
{
    /// <summary>
    /// A texture swapped per frame, playing at <see cref="FramesPerSecond"/>
    /// and looping on the last frame. Frames cut, they never blend.
    /// </summary>
    public class FlipbookExtras
    {
        public const int NoTexture = -1;

        [JsonProperty("framesPerSecond")] [DefaultValue(AnimationRate.FramesPerSecond)]
        public float FramesPerSecond = AnimationRate.FramesPerSecond;

        [JsonProperty("frames")] public int[] Frames;
    }
}
