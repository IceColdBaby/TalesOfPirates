using NUnit.Framework;
using Top.Assets.Conversion.Textures;
using Top.MindPower;
using Top.MindPower.Textures;

namespace Top.Assets.Conversion.Tests
{
    public class TextureFilterTests
    {
        [Test]
        public void Color_key_makes_exact_matches_transparent()
        {
            var pixels = new[]
            {
                new Rgba32(255, 0, 255, 255),
                new Rgba32(10, 20, 30, 255),
            };
            var image = new RgbaImage(2, 1, pixels);

            ColorKeyFilter.Apply(image, new Rgba32(255, 0, 255, 255));

            Assert.That(image.Pixels[0].A, Is.EqualTo(0));
            Assert.That(image.Pixels[1].A, Is.EqualTo(255));
        }

        [Test]
        public void Dilation_bleeds_opaque_color_into_keyed_pixels()
        {
            var pixels = new[]
            {
                new Rgba32(255, 200, 0, 255),
                new Rgba32(255, 0, 255, 255),
                new Rgba32(255, 0, 255, 255),
            };
            var image = new RgbaImage(3, 1, pixels);
            ColorKeyFilter.Apply(image, new Rgba32(255, 0, 255, 255));
            AlphaDilation.Apply(image, passes: 4);

            Assert.That(image.Pixels[1].A, Is.EqualTo(0));
            Assert.That(image.Pixels[1].R, Is.EqualTo(255));
            Assert.That(image.Pixels[1].G, Is.EqualTo(200));
            Assert.That(image.Pixels[2].G, Is.EqualTo(200));
        }
    }
}
