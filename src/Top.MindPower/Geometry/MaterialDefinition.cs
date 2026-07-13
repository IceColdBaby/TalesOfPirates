namespace Top.MindPower.Geometry
{
    /// <summary>
    /// D3DMATERIAL9 color set.
    /// <br/> lwMaterial (lwITypes.h)
    /// </summary>
    public struct MaterialDefinition
    {
        public RgbaF Diffuse;
        public RgbaF Ambient;
        public RgbaF Specular;
        public RgbaF Emissive;
        public float Power;
    }
}
