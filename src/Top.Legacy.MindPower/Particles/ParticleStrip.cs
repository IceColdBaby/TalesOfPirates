namespace Top.Legacy.MindPower.Particles
{
    /// <summary>
    /// Strip/ribbon trail block.
    /// <br/> CMPStrip::LoadFromFile/SaveToFile (MPModelEff.cpp)
    /// </summary>
    public class ParticleStrip
    {
        public int MaxLength;

        /// Length 2: start/end dummy bone indices.
        public int[] Dummy;

        public RgbaF Color;
        public float Life;
        public float Step;
        public string TextureName;
        public int SourceBlend;
        public int DestinationBlend;
    }
}
