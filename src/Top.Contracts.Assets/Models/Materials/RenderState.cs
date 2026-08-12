namespace Top.Contracts.Assets.Models.Materials
{
    /// <summary>
    /// Represents the fully resolved state for rendering a material,
    /// abstracted from any specific rendering engine.
    /// </summary>
    public class RenderState
    {
        public string TextureFile;
        public BlendFactor SrcBlend = BlendFactor.One;
        public BlendFactor DstBlend = BlendFactor.Zero;
        public bool BlendEnabled;
        public bool ZWrite = true;
        public FaceCulling Cull = FaceCulling.Back;
        public bool AlphaTest;
        public float Cutoff = 0.5f;
        public bool Lit = true;
        public float Opacity = 1f;
        public bool UvAnimated;
        public bool OpacityAnimated;
        public TransparencyMode Transparency;

        public bool OpacityDriven => OpacityAnimated || Opacity != 1f;

        public bool Blended => BlendEnabled || OpacityDriven;

        public BlendFactor EffectiveSrcBlend =>
            !BlendEnabled
                ? OpacityDriven ? BlendFactor.SrcAlpha : BlendFactor.One
                : OpacityDriven && Transparency == TransparencyMode.Additive
                    ? BlendFactor.SrcAlpha
                    : SrcBlend;

        public BlendFactor EffectiveDstBlend =>
            BlendEnabled
                ? DstBlend
                : OpacityDriven ? BlendFactor.OneMinusSrcAlpha : BlendFactor.Zero;

        public bool EffectiveZWrite => ZWrite && !OpacityDriven;
    }
}
