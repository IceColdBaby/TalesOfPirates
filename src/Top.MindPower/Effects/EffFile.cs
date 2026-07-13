using System.IO;
using System.Numerics;
using System.Text;

namespace Top.MindPower.Effects
{
    /// <summary>
    /// A .eff model-effect file.
    /// <br/> I_Effect::Load/Save (I_Effect.cpp)
    /// </summary>
    public sealed class EffFile
    {
        private const uint VersionMin = 1;
        private const uint VersionMax = 7;
        private const int MaxEffects = 1024;

        public uint Version;
        public int TechniqueIndex;
        public byte UsePath;
        public string PathName;
        public byte UseSound;
        public string SoundName;
        public byte Rotating;
        public Vector3 RotationAxis;
        public float RotationVelocity;
        public Effect[] Effects;

        public static EffFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var version = r.ReadUInt32();

            if (version < VersionMin || version > VersionMax)
            {
                throw new ParseException("eff", version, r.BaseStream.Position,
                    $"unsupported version {version}");
            }

            var file = new EffFile
            {
                Version = version,
                TechniqueIndex = r.ReadInt32(),
                UsePath = r.ReadByte(),
                PathName = r.ReadFixedString(EffSerialization.NameBytes),
                UseSound = r.ReadByte(),
                SoundName = r.ReadFixedString(EffSerialization.NameBytes),
                Rotating = r.ReadByte(),
                RotationAxis = r.ReadVector3(),
                RotationVelocity = r.ReadSingle(),
            };

            var effectCount = r.ReadInt32();
            if (effectCount < 0 || effectCount > MaxEffects)
            {
                throw new ParseException("eff", version, r.BaseStream.Position,
                    $"implausible effect_count {effectCount}");
            }

            file.Effects = new Effect[effectCount];

            for (var i = 0; i < effectCount; i++)
            {
                file.Effects[i] = r.ReadEffect(version);
            }

            return file;
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write(Version);
            w.Write(TechniqueIndex);
            w.Write(UsePath);
            w.WriteFixedString(PathName, EffSerialization.NameBytes);
            w.Write(UseSound);
            w.WriteFixedString(SoundName, EffSerialization.NameBytes);
            w.Write(Rotating);
            w.Write(RotationAxis);
            w.Write(RotationVelocity);

            w.Write(Effects.Length);

            foreach (var effect in Effects)
            {
                w.WriteEffect(effect, Version);
            }

            w.Flush();
        }
    }
}
