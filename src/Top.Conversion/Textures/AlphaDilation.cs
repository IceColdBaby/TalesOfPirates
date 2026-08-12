using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Textures;

namespace Top.Conversion.Textures
{
    /// <summary>
    /// Bleeds the nearest opaque color into fully transparent texels so that
    /// bilinear filtering and mipmaps do not darken alpha-tested edges.
    /// </summary>
    public static class AlphaDilation
    {
        public static void Apply(RgbaImage image, int passes)
        {
            var filled = new bool[image.Pixels.Length];

            for (var i = 0; i < filled.Length; i++)
            {
                filled[i] = image.Pixels[i].A != 0;
            }

            for (var pass = 0; pass < passes; pass++)
            {
                var next = (bool[])filled.Clone();
                var changed = false;

                for (var y = 0; y < image.Height; y++)
                {
                    for (var x = 0; x < image.Width; x++)
                    {
                        var index = (y * image.Width) + x;

                        if (filled[index])
                        {
                            continue;
                        }

                        int r = 0, g = 0, b = 0, count = 0;

                        for (var dy = -1; dy <= 1; dy++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var nx = x + dx;
                                var ny = y + dy;

                                if (nx < 0 || ny < 0 || nx >= image.Width || ny >= image.Height)
                                {
                                    continue;
                                }

                                var neighbor = (ny * image.Width) + nx;

                                if (!filled[neighbor])
                                {
                                    continue;
                                }

                                r += image.Pixels[neighbor].R;
                                g += image.Pixels[neighbor].G;
                                b += image.Pixels[neighbor].B;

                                count++;
                            }
                        }

                        if (count == 0)
                        {
                            continue;
                        }

                        image.Pixels[index] = new Rgba32((byte)(r / count), (byte)(g / count), (byte)(b / count), 0);
                        next[index] = true;
                        changed = true;
                    }
                }

                filled = next;

                if (!changed)
                {
                    break;
                }
            }
        }
    }
}
