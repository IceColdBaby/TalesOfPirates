namespace Top.MindPower.Geometry
{
    /// <summary>
    /// Container of helper sub-blocks; Type is the bit-mask of present sequences.
    /// <br/> lwHelperInfo (lwExpObj.h)
    /// </summary>
    public class Helper
    {
        public HelperType Type;
        public HelperDummy[] Dummies;
        public HelperBox[] Boxes;
        public HelperMesh[] Meshes;
        public BoundingBox[] BoundingBoxes;
        public BoundingSphere[] BoundingSpheres;
    }
}
