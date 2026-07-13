namespace Top.MindPower.Geometry
{
    /// <summary>
    /// Packed bone-index word + 4 blend weights for a skinned vertex.
    /// <br/> lwBlendInfo (lwITypes2.h)
    /// </summary>
    public struct SkinBlend
    {
        public uint BoneIndex;
        public float Weight0;
        public float Weight1;
        public float Weight2;
        public float Weight3;
    }
}
