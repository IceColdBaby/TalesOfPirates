using System.IO;
using System.Numerics;
using Top.MindPower.Geometry;

namespace Top.MindPower.Particles
{
    /// <summary>
    /// Emitter/path/strip/model blocks of a .par file.
    /// <br/> CMPPartSys/CMPStrip/CChaModel/CEffPath (MPParticleSys.cpp / MPModelEff.cpp / MPParticleCtrl.cpp)
    /// </summary>
    internal static class ParSerialization
    {
        internal const int NameBytes = 32;

        internal static ParticleEmitter ReadEmitter(this BinaryReader r, uint version)
        {
            var p = new ParticleEmitter
            {
                Type = (ParticleSystemType)r.ReadInt32(),
                PartName = r.ReadFixedString(NameBytes),
                ParticleCount = r.ReadInt32(),
                TextureName = r.ReadFixedString(NameBytes),
                ModelName = r.ReadFixedString(NameBytes),
                Range = r.ReadFloats(3),
            };

            var frameCount = r.ReadUInt16();
            p.FrameSize = r.ReadFloats(frameCount);
            p.FrameAngle = r.ReadVec3Array(frameCount);
            p.FrameColor = r.ReadColors(frameCount);
            p.Billboard = r.ReadByte();
            p.SourceBlend = r.ReadInt32();
            p.DestinationBlend = r.ReadInt32();
            p.MinFilter = r.ReadInt32();
            p.MagFilter = r.ReadInt32();
            p.Life = r.ReadSingle();
            p.Velocity = r.ReadSingle();
            p.Direction = r.ReadVector3();
            p.Acceleration = r.ReadVector3();
            p.Step = r.ReadSingle();

            if (version > 3)
            {
                p.ModelRangeFlag = r.ReadByte();
                p.ModelRangeName = r.ReadFixedString(NameBytes);
            }

            if (version > 4)
            {
                p.Offset = r.ReadVector3();
            }

            if (version > 5)
            {
                p.DelayTime = r.ReadSingle();
                p.PlayTime = r.ReadSingle();
            }

            if (version > 8)
            {
                p.UsePath = r.ReadByte();
                if (p.UsePath != 0)
                {
                    p.Path = r.ReadPath();
                }
            }

            if (version > 9)
            {
                p.Shade = r.ReadByte();
            }

            if (version > 10)
            {
                p.HitEffectName = r.ReadFixedString(NameBytes);
            }

            if (version > 11 && p.ModelRangeFlag != 0)
            {
                p.PointRanges = r.ReadVec3Array(r.ReadUInt16());
            }

            if (version > 12)
            {
                p.Roadom = r.ReadInt32();
            }

            if (version > 13)
            {
                p.ModelDirection = r.ReadByte();
            }

            if (version > 14)
            {
                p.Mediay = r.ReadByte();
            }

            return p;
        }

        internal static void WriteEmitter(this BinaryWriter w, ParticleEmitter p, uint version)
        {
            w.Write((int)p.Type);
            w.WriteFixedString(p.PartName, NameBytes);
            w.Write(p.ParticleCount);
            w.WriteFixedString(p.TextureName, NameBytes);
            w.WriteFixedString(p.ModelName, NameBytes);
            w.WriteFloats(p.Range);

            w.Write((ushort)p.FrameSize.Length);
            w.WriteFloats(p.FrameSize);
            w.WriteVec3Array(p.FrameAngle);
            w.WriteColors(p.FrameColor);
            w.Write(p.Billboard);
            w.Write(p.SourceBlend);
            w.Write(p.DestinationBlend);
            w.Write(p.MinFilter);
            w.Write(p.MagFilter);
            w.Write(p.Life);
            w.Write(p.Velocity);
            w.Write(p.Direction);
            w.Write(p.Acceleration);
            w.Write(p.Step);

            if (version > 3)
            {
                w.Write(p.ModelRangeFlag);
                w.WriteFixedString(p.ModelRangeName, NameBytes);
            }

            if (version > 4)
            {
                w.Write(p.Offset);
            }

            if (version > 5)
            {
                w.Write(p.DelayTime);
                w.Write(p.PlayTime);
            }

            if (version > 8)
            {
                w.Write(p.UsePath);
                if (p.UsePath != 0)
                {
                    w.WritePath(p.Path);
                }
            }

            if (version > 9)
            {
                w.Write(p.Shade);
            }

            if (version > 10)
            {
                w.WriteFixedString(p.HitEffectName, NameBytes);
            }

            if (version > 11 && p.ModelRangeFlag != 0)
            {
                w.Write((ushort)p.PointRanges.Length);
                w.WriteVec3Array(p.PointRanges);
            }

            if (version > 12)
            {
                w.Write(p.Roadom);
            }

            if (version > 13)
            {
                w.Write(p.ModelDirection);
            }

            if (version > 14)
            {
                w.Write(p.Mediay);
            }
        }

        private static EffectPath ReadPath(this BinaryReader r)
        {
            var frameCount = r.ReadInt32();
            var path = new EffectPath
            {
                Velocity = r.ReadSingle(),
                PathPoints = r.ReadVec3Array(frameCount),
            };

            var segments = frameCount > 0 ? frameCount - 1 : 0;
            path.Directions = new Vector3[segments];
            path.Distances = new EffectPathDistanceSlot[segments];
            for (var i = 0; i < segments; i++)
            {
                path.Directions[i] = r.ReadVector3();
                path.Distances[i] = new EffectPathDistanceSlot
                {
                    Value = r.ReadSingle(),
                    Pad0 = r.ReadSingle(),
                    Pad1 = r.ReadSingle(),
                };
            }

            return path;
        }

        private static void WritePath(this BinaryWriter w, EffectPath path)
        {
            w.Write(path.PathPoints.Length);
            w.Write(path.Velocity);
            w.WriteVec3Array(path.PathPoints);
            for (var i = 0; i < path.Directions.Length; i++)
            {
                w.Write(path.Directions[i]);
                var slot = path.Distances[i];
                w.Write(slot.Value);
                w.Write(slot.Pad0);
                w.Write(slot.Pad1);
            }
        }

        internal static ParticleStrip ReadStrip(this BinaryReader r)
        {
            return new ParticleStrip
            {
                MaxLength = r.ReadInt32(),
                Dummy = new[] { r.ReadInt32(), r.ReadInt32() },
                Color = r.ReadRgbaF(),
                Life = r.ReadSingle(),
                Step = r.ReadSingle(),
                TextureName = r.ReadFixedString(NameBytes),
                SourceBlend = r.ReadInt32(),
                DestinationBlend = r.ReadInt32(),
            };
        }

        internal static void WriteStrip(this BinaryWriter w, ParticleStrip s)
        {
            w.Write(s.MaxLength);
            w.Write(s.Dummy[0]);
            w.Write(s.Dummy[1]);
            w.Write(s.Color);
            w.Write(s.Life);
            w.Write(s.Step);
            w.WriteFixedString(s.TextureName, NameBytes);
            w.Write(s.SourceBlend);
            w.Write(s.DestinationBlend);
        }

        internal static ParticleModel ReadModel(this BinaryReader r)
        {
            return new ParticleModel
            {
                Id = r.ReadInt32(),
                Velocity = r.ReadSingle(),
                PlayType = (AnimationPlayType)r.ReadInt32(),
                CurrentPose = r.ReadInt32(),
                SourceBlend = r.ReadInt32(),
                DestinationBlend = r.ReadInt32(),
                CurrentColor = r.ReadRgbaF(),
            };
        }

        internal static void WriteModel(this BinaryWriter w, ParticleModel m)
        {
            w.Write(m.Id);
            w.Write(m.Velocity);
            w.Write((int)m.PlayType);
            w.Write(m.CurrentPose);
            w.Write(m.SourceBlend);
            w.Write(m.DestinationBlend);
            w.Write(m.CurrentColor);
        }
    }
}
