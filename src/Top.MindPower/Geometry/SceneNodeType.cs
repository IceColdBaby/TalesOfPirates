namespace Top.MindPower.Geometry
{
    /// <summary>
    /// Node kind in a .lxo scene tree.
    /// <br/> lwModelNodeInfo::Load (lwExpObj.cpp)
    /// </summary>
    public enum SceneNodeType : uint
    {
        Primitive = 1,
        BoneCtrl = 2,
        Dummy = 3,
        Helper = 4,
    }
}
