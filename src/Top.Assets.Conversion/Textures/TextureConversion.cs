using System.IO;
using Top.MindPower.Geometry;
using Top.MindPower.Textures;

namespace Top.Assets.Conversion.Textures
{
    /// <summary>
    /// Turns a legacy texture file into a Unity-ready PNG: decode by content,
    /// knock out the stage's color key, bleed the remaining color into the
    /// transparent texels, and encode.
    /// </summary>
    public static class TextureConversion
    {
        private const int DilationPasses = 4;

        /// <summary>
        /// Throws <see cref="InvalidDataException"/> when the source is not a
        /// texture the readers recognize.
        /// </summary>
        public static byte[] ToPng(byte[] source, TextureStage stage)
        {
            var image = TextureReader.Read(source);

            if (stage.ColorKeyType == ColorKeyType.Specific)
            {
                ColorKeyFilter.Apply(image, stage.ColorKey);
            }
            else if (stage.ColorKeyType == ColorKeyType.FirstPixel)
            {
                ColorKeyFilter.Apply(image, image.Pixels[0]);
            }

            AlphaDilation.Apply(image, DilationPasses);

            return PngWriter.Write(image);
        }
    }
}
