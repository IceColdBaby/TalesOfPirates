namespace Top.Legacy.MindPower.World
{
    /// <summary>
    /// One placed scene object. X and Y are centimeters from the corner of the
    /// section holding the object, not from the corner of the map.
    /// <br/> SSceneObjInfo (SceneObjFile.h)
    /// </summary>
    public class SceneObject
    {
        public short TypeId;
        public int X;
        public int Y;
        public short HeightOff;
        public short YawAngle;
        public short Scale;
    }
}
