using Top.MindPower;
using Top.MindPower.Textures;

namespace Top.Assets.Conversion.Textures
{
    public static class ColorKeyFilter
    {
        public static void Apply(RgbaImage image, Rgba32 key)
        {
            for (var i = 0; i < image.Pixels.Length; i++)
            {
                var p = image.Pixels[i];

                if (p.R == key.R && p.G == key.G && p.B == key.B)
                {
                    image.Pixels[i] = new Rgba32(0, 0, 0, 0);
                }
            }
        }
    }
}
