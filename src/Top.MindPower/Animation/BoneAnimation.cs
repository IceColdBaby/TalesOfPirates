namespace Top.MindPower.Animation
{
    /// <summary>
    /// Parsed .lab bone animation.
    /// <br/> lwAnimDataBone (lwExpObj.h)
    /// </summary>
    public class BoneAnimation
    {
        public uint Version;
        public int FrameCount;
        public BoneKeyType KeyType;
        public Bone[] Bones;
        public BoneDummy[] Dummies;
        public BoneTrack[] Tracks;
    }
}
