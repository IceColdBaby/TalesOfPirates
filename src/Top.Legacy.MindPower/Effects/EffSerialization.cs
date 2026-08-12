using System.IO;
using System.Numerics;

namespace Top.Legacy.MindPower.Effects
{
    /// <summary>
    /// One sub-effect's on-disk block.
    /// <br/> I_Effect::LoadFromFile/Save (I_Effect.cpp)
    /// </summary>
    internal static class EffSerialization
    {
        internal const int NameBytes = 32;

        internal static Effect ReadEffect(this BinaryReader r, uint version)
        {
            var e = new Effect
            {
                Name = r.ReadFixedString(NameBytes),
                EffectType = (EffectType)r.ReadInt32(),
                SourceBlend = r.ReadInt32(),
                DestinationBlend = r.ReadInt32(),
                Length = r.ReadSingle(),
            };

            var frameCount = r.ReadUInt16();
            e.FrameTime = r.ReadFloats(frameCount);
            e.FrameSize = r.ReadVec3Array(frameCount);
            e.FrameAngle = r.ReadVec3Array(frameCount);
            e.FramePosition = r.ReadVec3Array(frameCount);
            e.FrameColor = r.ReadColors(frameCount);

            e.TextureCoordinateVertexCount = r.ReadUInt16();
            var textureCoordinateListCount = r.ReadUInt16();
            e.TextureCoordinateFrameTime = r.ReadSingle();
            e.TextureCoordinateLists = new Vector2[textureCoordinateListCount][];
            for (var i = 0; i < textureCoordinateListCount; i++)
            {
                e.TextureCoordinateLists[i] = r.ReadVec2Array(e.TextureCoordinateVertexCount);
            }

            var texCount = r.ReadUInt16();
            e.TextureFrameTime = r.ReadSingle();
            e.TextureName = r.ReadFixedString(NameBytes);
            e.TextureLists = new Vector2[texCount][];
            for (var i = 0; i < texCount; i++)
            {
                e.TextureLists[i] = r.ReadVec2Array(e.TextureCoordinateVertexCount);
            }

            e.ModelName = r.ReadFixedString(NameBytes);
            e.Billboard = r.ReadByte();
            e.VertexShaderIndex = r.ReadInt32();

            if (version > 1)
            {
                e.SegmentCount = r.ReadInt32();
                e.Height = r.ReadSingle();
                e.TopRadius = r.ReadSingle();
                e.BottomRadius = r.ReadSingle();
            }

            if (version > 2)
            {
                var texFrameCount = r.ReadUInt16();
                e.TextureFrameTimeA = r.ReadSingle();
                e.TextureFrameNames = new string[texFrameCount];
                for (var i = 0; i < texFrameCount; i++)
                {
                    e.TextureFrameNames[i] = r.ReadFixedString(NameBytes);
                }

                e.TextureFrameTimeB = r.ReadSingle();
            }

            if (version > 3)
            {
                e.UseParameter = r.ReadInt32();
                if (e.UseParameter > 0)
                {
                    e.CylinderParameters = new EffectCylinderParameters[frameCount];
                    for (var i = 0; i < frameCount; i++)
                    {
                        e.CylinderParameters[i] = new EffectCylinderParameters
                        {
                            Segments = r.ReadInt32(),
                            Height = r.ReadSingle(),
                            TopRadius = r.ReadSingle(),
                            BottomRadius = r.ReadSingle(),
                        };
                    }
                }
            }

            if (version > 4)
            {
                e.RotationLoop = r.ReadByte();
                e.RotationLoopVector = r.ReadVector4();
            }

            if (version > 5)
            {
                e.Alpha = r.ReadByte();
            }

            if (version > 6)
            {
                e.RotationBoard = r.ReadByte();
            }

            return e;
        }

        internal static void WriteEffect(this BinaryWriter w, Effect e, uint version)
        {
            w.WriteFixedString(e.Name, NameBytes);
            w.Write((int)e.EffectType);
            w.Write(e.SourceBlend);
            w.Write(e.DestinationBlend);
            w.Write(e.Length);

            var frameCount = (ushort)e.FrameTime.Length;
            w.Write(frameCount);
            w.WriteFloats(e.FrameTime);
            w.WriteVec3Array(e.FrameSize);
            w.WriteVec3Array(e.FrameAngle);
            w.WriteVec3Array(e.FramePosition);
            w.WriteColors(e.FrameColor);

            w.Write(e.TextureCoordinateVertexCount);
            w.Write((ushort)e.TextureCoordinateLists.Length);
            w.Write(e.TextureCoordinateFrameTime);
            foreach (var list in e.TextureCoordinateLists)
            {
                w.WriteVec2Array(list);
            }

            w.Write((ushort)e.TextureLists.Length);
            w.Write(e.TextureFrameTime);
            w.WriteFixedString(e.TextureName, NameBytes);
            foreach (var list in e.TextureLists)
            {
                w.WriteVec2Array(list);
            }

            w.WriteFixedString(e.ModelName, NameBytes);
            w.Write(e.Billboard);
            w.Write(e.VertexShaderIndex);

            if (version > 1)
            {
                w.Write(e.SegmentCount);
                w.Write(e.Height);
                w.Write(e.TopRadius);
                w.Write(e.BottomRadius);
            }

            if (version > 2)
            {
                w.Write((ushort)e.TextureFrameNames.Length);
                w.Write(e.TextureFrameTimeA);
                foreach (var name in e.TextureFrameNames)
                {
                    w.WriteFixedString(name, NameBytes);
                }

                w.Write(e.TextureFrameTimeB);
            }

            if (version > 3)
            {
                w.Write(e.UseParameter);
                if (e.UseParameter > 0)
                {
                    for (var i = 0; i < frameCount; i++)
                    {
                        var c = e.CylinderParameters[i];
                        w.Write(c.Segments);
                        w.Write(c.Height);
                        w.Write(c.TopRadius);
                        w.Write(c.BottomRadius);
                    }
                }
            }

            if (version > 4)
            {
                w.Write(e.RotationLoop);
                w.Write(e.RotationLoopVector);
            }

            if (version > 5)
            {
                w.Write(e.Alpha);
            }

            if (version > 6)
            {
                w.Write(e.RotationBoard);
            }
        }
    }
}
