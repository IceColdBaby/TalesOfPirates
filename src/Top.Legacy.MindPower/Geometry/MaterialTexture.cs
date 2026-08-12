namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// A material plus its (up to 4) texture stages.
    /// <br/> lwMtlTexInfo (lwITypes2.h)
    /// </summary>
    public class MaterialTexture
    {
        public float Opacity = 1f;
        public TransparencyType Transparency;
        public MaterialDefinition Material;
        public RenderStateAtom[] RenderStates;
        public TextureStage[] Stages;
    }
}
