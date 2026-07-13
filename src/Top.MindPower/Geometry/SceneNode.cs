namespace Top.MindPower.Geometry
{
    /// <summary>
    /// One .lxo node.
    /// <br/> lwModelNodeInfo (lwExpObj.h)
    /// </summary>
    public class SceneNode
    {
        public SceneNodeType Type;
        public uint Handle;
        public uint Id;
        public string Name;
        public uint ParentHandle;
        public uint LinkParentId;
        public uint LinkId;
        public object Data;
    }
}
