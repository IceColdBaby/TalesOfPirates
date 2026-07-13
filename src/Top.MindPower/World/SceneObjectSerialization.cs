using System.IO;

namespace Top.MindPower.World
{
    /// <summary>
    /// One placed object's record.
    /// <br/> SSceneObjInfo (SceneObjFile.h)
    /// </summary>
    internal static class SceneObjectSerialization
    {
        internal const int ObjectBytes = 20;

        internal static SceneObject ReadSceneObject(this BinaryReader r)
        {
            var obj = new SceneObject
            {
                TypeId = r.ReadInt16(),
            };

            r.ReadInt16();
            obj.X = r.ReadInt32();
            obj.Y = r.ReadInt32();
            obj.HeightOff = r.ReadInt16();
            obj.YawAngle = r.ReadInt16();
            obj.Scale = r.ReadInt16();
            r.ReadInt16();

            return obj;
        }

        internal static void WriteSceneObject(this BinaryWriter w, SceneObject obj)
        {
            w.Write(obj.TypeId);
            w.Write((short)0);
            w.Write(obj.X);
            w.Write(obj.Y);
            w.Write(obj.HeightOff);
            w.Write(obj.YawAngle);
            w.Write(obj.Scale);
            w.Write((short)0);
        }
    }
}
