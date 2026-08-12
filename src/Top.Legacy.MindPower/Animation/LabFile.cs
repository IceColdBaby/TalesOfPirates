using System.IO;
using System.Text;

namespace Top.Legacy.MindPower.Animation
{
    /// <summary>
    /// Standalone .lab bone-animation file.
    /// <br/> lwAnimDataBone::Load/Save (lwExpObj.cpp)
    /// </summary>
    public class LabFile
    {
        private const uint VersionMin = 0x1000;

        public BoneAnimation Animation;

        public static LabFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var version = r.ReadUInt32();

            if (version != 1 && version < VersionMin)
            {
                throw new ParseException("lab", version, r.BaseStream.Position,
                    $"unsupported version 0x{version:X4}");
            }

            return new LabFile { Animation = r.ReadBoneAnimation(version) };
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write(Animation.Version);
            w.WriteBoneAnimation(Animation);
            w.Flush();
        }
    }
}
