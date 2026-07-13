namespace Top.MindPower
{
    /// <summary>
    /// Float RGBA material color.
    /// <br/> lwColorValue4f (lwITypes.h)
    /// </summary>
    public readonly struct RgbaF
    {
        public readonly float R, G, B, A;

        public RgbaF(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}
