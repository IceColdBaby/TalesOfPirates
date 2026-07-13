using System.IO;
using System.Numerics;

namespace Top.MindPower.Animation
{
    /// <summary>
    /// lwAnimDataBone::Load/Save (lwExpObj.cpp)
    /// </summary>
    internal static class BoneAnimationSerialization
    {
        private const uint InvalidIndex = 0xFFFFFFFF;
        private const uint VersionQuatAllFrames = 0x1003;

        internal static BoneAnimation ReadBoneAnimation(this BinaryReader r, uint version)
        {
            var boneNum = r.ReadUInt32();
            var frameNum = r.ReadUInt32();
            var dummyNum = r.ReadUInt32();
            var keyType = (BoneKeyType)r.ReadUInt32();

            if (boneNum > 512 || frameNum > 65536)
            {
                throw new ParseException("lab", version, r.BaseStream.Position,
                    $"implausible bone/frame counts: {boneNum}/{frameNum}");
            }

            var bones = ReadBoneBaseInfos(r, (int)boneNum);

            ReadInvBindMatrices(r, bones);

            var dummies = ReadDummies(r, (int)dummyNum);
            var tracks = ReadTracks(r, version, (int)boneNum, (int)frameNum, keyType, bones);

            return new BoneAnimation
            {
                Version = version,
                FrameCount = (int)frameNum,
                KeyType = keyType,
                Bones = bones,
                Dummies = dummies,
                Tracks = tracks,
            };
        }

        internal static void WriteBoneAnimation(this BinaryWriter w, BoneAnimation animation)
        {
            w.Write((uint)animation.Bones.Length);
            w.Write((uint)animation.FrameCount);
            w.Write((uint)animation.Dummies.Length);
            w.Write((uint)animation.KeyType);

            WriteBoneBaseInfos(w, animation.Bones);
            WriteInvBindMatrices(w, animation.Bones);
            WriteDummies(w, animation.Dummies);
            WriteTracks(w, animation);
        }

        private static Bone[] ReadBoneBaseInfos(BinaryReader r, int count)
        {
            var arr = new Bone[count];

            for (var i = 0; i < count; i++)
            {
                var name = r.ReadFixedString(64);
                var id = r.ReadUInt32();
                var parentId = r.ReadUInt32();
                arr[i] = new Bone
                {
                    Name = name, Id = (int)id, ParentId = parentId == InvalidIndex ? -1 : (int)parentId,
                };
            }

            return arr;
        }

        private static void ReadInvBindMatrices(BinaryReader r, Bone[] bones)
        {
            foreach (var bone in bones)
            {
                bone.InvBindMatrix = RowMajor(r.ReadMatrix44Raw());
            }
        }

        private static BoneDummy[] ReadDummies(BinaryReader r, int count)
        {
            var arr = new BoneDummy[count];

            for (var i = 0; i < count; i++)
            {
                var id = r.ReadUInt32();
                var parentBoneId = r.ReadUInt32();
                var mat = RowMajor(r.ReadMatrix44Raw());
                arr[i] = new BoneDummy { Id = id, ParentBoneId = parentBoneId, Matrix = mat };
            }

            return arr;
        }

        private static BoneTrack[] ReadTracks(BinaryReader r, uint version,
            int boneNum, int frameNum, BoneKeyType keyType, Bone[] bones)
        {
            var tracks = new BoneTrack[boneNum];

            switch (keyType)
            {
                case BoneKeyType.Mat43:
                    for (var b = 0; b < boneNum; b++)
                    {
                        var frames = new Matrix4x4[frameNum];

                        for (var f = 0; f < frameNum; f++)
                        {
                            var m43 = r.ReadFloats(12);
                            frames[f] = RowMajor(new[]
                            {
                                m43[0], m43[1], m43[2], 0f, m43[3], m43[4], m43[5], 0f, m43[6], m43[7], m43[8], 0f,
                                m43[9], m43[10], m43[11], 1f,
                            });
                        }

                        tracks[b] = new MatrixBoneTrack { Frames = frames };
                    }

                    break;

                case BoneKeyType.Mat44:
                    for (var b = 0; b < boneNum; b++)
                    {
                        var frames = new Matrix4x4[frameNum];

                        for (var f = 0; f < frameNum; f++)
                        {
                            frames[f] = RowMajor(r.ReadMatrix44Raw());
                        }

                        tracks[b] = new MatrixBoneTrack { Frames = frames };
                    }

                    break;

                case BoneKeyType.Quat:
                    if (version >= VersionQuatAllFrames)
                    {
                        for (var b = 0; b < boneNum; b++)
                        {
                            var positions = r.ReadVec3Array(frameNum);
                            var rotations = r.ReadQuaternionArray(frameNum);
                            tracks[b] = new QuaternionBoneTrack { Positions = positions, Rotations = rotations };
                        }
                    }
                    else
                    {
                        for (var b = 0; b < boneNum; b++)
                        {
                            var isRoot = bones[b].ParentId == -1;
                            var posCount = isRoot ? frameNum : 1;
                            var positions = r.ReadVec3Array(posCount);
                            var rotations = r.ReadQuaternionArray(frameNum);
                            tracks[b] = new QuaternionBoneTrack { Positions = positions, Rotations = rotations };
                        }
                    }

                    break;

                default:
                    throw new ParseException("lab", version, r.BaseStream.Position,
                        $"unknown key type {(uint)keyType}");
            }

            return tracks;
        }

        private static void WriteBoneBaseInfos(BinaryWriter w, Bone[] bones)
        {
            foreach (var bone in bones)
            {
                w.WriteFixedString(bone.Name, 64);
                w.Write((uint)bone.Id);
                w.Write(bone.ParentId == -1 ? InvalidIndex : (uint)bone.ParentId);
            }
        }

        private static void WriteInvBindMatrices(BinaryWriter w, Bone[] bones)
        {
            foreach (var bone in bones)
            {
                w.WriteMatrix44Raw(ToRowMajor(bone.InvBindMatrix));
            }
        }

        private static void WriteDummies(BinaryWriter w, BoneDummy[] dummies)
        {
            for (var i = 0; i < dummies.Length; i++)
            {
                w.Write(dummies[i].Id);
                w.Write(dummies[i].ParentBoneId);
                w.WriteMatrix44Raw(ToRowMajor(dummies[i].Matrix));
            }
        }

        private static void WriteTracks(BinaryWriter w, BoneAnimation animation)
        {
            switch (animation.KeyType)
            {
                case BoneKeyType.Mat43:
                    foreach (var boneTrack in animation.Tracks)
                    {
                        var frames = ((MatrixBoneTrack)boneTrack).Frames;

                        foreach (var frame in frames)
                        {
                            var m = ToRowMajor(frame);
                            w.WriteFloats(new[]
                            {
                                m[0], m[1], m[2], m[4], m[5], m[6], m[8], m[9], m[10], m[12], m[13], m[14],
                            });
                        }
                    }

                    break;

                case BoneKeyType.Mat44:
                    foreach (var boneTrack in animation.Tracks)
                    {
                        var frames = ((MatrixBoneTrack)boneTrack).Frames;

                        foreach (var frame in frames)
                        {
                            w.WriteMatrix44Raw(ToRowMajor(frame));
                        }
                    }

                    break;

                case BoneKeyType.Quat:
                    foreach (var boneTrack in animation.Tracks)
                    {
                        var track = (QuaternionBoneTrack)boneTrack;
                        w.WriteVec3Array(track.Positions);
                        w.WriteQuaternionArray(track.Rotations);
                    }

                    break;

                default:
                    throw new ParseException("lab", animation.Version, w.BaseStream.Position,
                        $"unknown key type {(uint)animation.KeyType}");
            }
        }

        private static Matrix4x4 RowMajor(float[] m)
        {
            return new Matrix4x4(
                m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7],
                m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
        }

        private static float[] ToRowMajor(Matrix4x4 m)
        {
            return new[]
            {
                m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42,
                m.M43, m.M44,
            };
        }
    }
}
