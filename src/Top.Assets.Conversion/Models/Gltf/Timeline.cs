using Top.Assets.Contract.Models;

namespace Top.Assets.Conversion.Models.Gltf
{
    /// <summary>
    /// When each frame of a clip is sampled.
    /// </summary>
    public static class Timeline
    {
        public static float[] Seconds(int frameCount)
        {
            var times = new float[frameCount];

            for (var i = 0; i < frameCount; i++)
            {
                times[i] = i / AnimationRate.FramesPerSecond;
            }

            return times;
        }
    }
}
