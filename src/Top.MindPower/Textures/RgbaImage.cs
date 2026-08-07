namespace Top.MindPower.Textures
{
    public class RgbaImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Rgba32[] Pixels;

        public RgbaImage(int width, int height, Rgba32[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }
    }
}
