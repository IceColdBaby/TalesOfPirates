using System.IO;
using System.Numerics;
using Top.MindPower.Animation;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// Bone, matrix, per-material opacity, and per-(subset,stage) texture tracks.
    /// <br/> lwAnimDataInfo::Load/Save (lwExpObj.cpp)
    /// </summary>
    internal static class AnimationDataSerialization
    {
        private const int SubsetNum = 16;
        private const int StageNum = 4;

        internal static AnimationData ReadAnimationData(this BinaryReader r, uint version)
        {
            var info = new AnimationData();

            if (version == 0)
            {
                r.ReadUInt32();
            }

            var dataBoneSize = r.ReadUInt32();
            var dataMatSize = r.ReadUInt32();

            var mtlOpacitySizes = new uint[SubsetNum];
            if (version >= 4101)
            {
                for (var s = 0; s < SubsetNum; s++)
                {
                    mtlOpacitySizes[s] = r.ReadUInt32();
                }
            }

            var texUvSizes = ReadSizeGrid(r);
            var texImgSizes = ReadSizeGrid(r);

            if (dataBoneSize > 0)
            {
                var boneVersion = version;

                if (boneVersion == 0)
                {
                    boneVersion = r.ReadUInt32();
                }

                info.Bone = r.ReadBoneAnimation(boneVersion);
            }

            if (dataMatSize > 0)
            {
                info.Matrix = ReadMatrixAnimation(r, version);
            }

            if (version >= 4101)
            {
                info.MaterialOpacity = new MaterialOpacityAnimation[SubsetNum];
                for (var subset = 0; subset < SubsetNum; subset++)
                {
                    if (mtlOpacitySizes[subset] == 0)
                    {
                        continue;
                    }

                    info.MaterialOpacity[subset] = ReadMaterialOpacity(r);
                }
            }

            info.TextureUv = new TextureUvAnimation[SubsetNum, StageNum];

            for (var subset = 0; subset < SubsetNum; subset++)
            for (var stage = 0; stage < StageNum; stage++)
            {
                if (texUvSizes[subset][stage] != 0)
                {
                    info.TextureUv[subset, stage] = ReadTextureUv(r, subset, stage);
                }
            }

            info.TextureImage = new TextureImageAnimation[SubsetNum, StageNum];

            for (var subset = 0; subset < SubsetNum; subset++)
            for (var stage = 0; stage < StageNum; stage++)
            {
                if (texImgSizes[subset][stage] != 0)
                {
                    info.TextureImage[subset, stage] = ReadTextureImage(r, subset, stage);
                }
            }

            return info;
        }

        internal static void WriteAnimationData(this BinaryWriter w, AnimationData anim, uint version)
        {
            if (version == 0)
            {
                w.Write(0u);
            }

            var boneBytes = anim.Bone != null
                ? GeometryObjectSerialization.Serialize(bw => WriteBoneBlock(bw, anim.Bone, version))
                : null;
            var matBytes = anim.Matrix != null
                ? GeometryObjectSerialization.Serialize(bw => WriteMatrixBlock(bw, anim.Matrix))
                : null;

            w.Write((uint)(boneBytes?.Length ?? 0));
            w.Write((uint)(matBytes?.Length ?? 0));

            byte[][] mtlOpacityBytes = null;
            if (version >= 4101)
            {
                mtlOpacityBytes = new byte[SubsetNum][];
                for (var s = 0; s < SubsetNum; s++)
                {
                    var track = anim.MaterialOpacity?[s];
                    mtlOpacityBytes[s] = track != null
                        ? GeometryObjectSerialization.Serialize(bw => WriteMaterialOpacity(bw, track))
                        : null;
                    w.Write((uint)(mtlOpacityBytes[s]?.Length ?? 0));
                }
            }

            var texUvBytes = new byte[SubsetNum][][];
            for (var s = 0; s < SubsetNum; s++)
            {
                texUvBytes[s] = new byte[StageNum][];
                for (var t = 0; t < StageNum; t++)
                {
                    var track = anim.TextureUv?[s, t];
                    texUvBytes[s][t] = track != null
                        ? GeometryObjectSerialization.Serialize(bw => WriteTextureUv(bw, track))
                        : null;
                    w.Write((uint)(texUvBytes[s][t]?.Length ?? 0));
                }
            }

            var texImgBytes = new byte[SubsetNum][][];
            for (var s = 0; s < SubsetNum; s++)
            {
                texImgBytes[s] = new byte[StageNum][];
                for (var t = 0; t < StageNum; t++)
                {
                    var track = anim.TextureImage?[s, t];
                    texImgBytes[s][t] = track != null
                        ? GeometryObjectSerialization.Serialize(bw => WriteTextureImage(bw, track))
                        : null;
                    w.Write((uint)(texImgBytes[s][t]?.Length ?? 0));
                }
            }

            if (boneBytes != null)
            {
                w.Write(boneBytes);
            }

            if (matBytes != null)
            {
                w.Write(matBytes);
            }

            if (mtlOpacityBytes != null)
            {
                foreach (var block in mtlOpacityBytes)
                {
                    if (block != null)
                    {
                        w.Write(block);
                    }
                }
            }

            WriteGrid(w, texUvBytes);
            WriteGrid(w, texImgBytes);
        }

        private static uint[][] ReadSizeGrid(BinaryReader r)
        {
            var grid = new uint[SubsetNum][];
            for (var s = 0; s < SubsetNum; s++)
            {
                grid[s] = new uint[StageNum];
                for (var t = 0; t < StageNum; t++)
                {
                    grid[s][t] = r.ReadUInt32();
                }
            }

            return grid;
        }

        private static void WriteGrid(BinaryWriter w, byte[][][] grid)
        {
            for (var s = 0; s < SubsetNum; s++)
            for (var t = 0; t < StageNum; t++)
            {
                if (grid[s][t] != null)
                {
                    w.Write(grid[s][t]);
                }
            }
        }

        private static MatrixAnimation ReadMatrixAnimation(BinaryReader r, uint version)
        {
            var frameNum = r.ReadUInt32();

            if (frameNum == 0 || frameNum >= 65536)
            {
                throw new ParseException("anim", version, r.BaseStream.Position,
                    $"implausible matrix-anim frame count {frameNum}");
            }

            var frames = new Matrix4x4[frameNum];

            for (var f = 0; f < (int)frameNum; f++)
            {
                frames[f] = r.ReadMatrix43();
            }

            return new MatrixAnimation { Frames = frames };
        }

        private static MaterialOpacityAnimation ReadMaterialOpacity(BinaryReader r)
        {
            var keyNum = r.ReadUInt32();
            var keys = new FloatKeyframe[keyNum];

            for (var k = 0; k < (int)keyNum; k++)
            {
                keys[k] = new FloatKeyframe
                {
                    Key = r.ReadUInt32(), SlerpType = r.ReadUInt32(), Value = r.ReadSingle(),
                };
            }

            return new MaterialOpacityAnimation { Keys = keys };
        }

        private static TextureUvAnimation ReadTextureUv(BinaryReader r, int subset, int stage)
        {
            var frameNum = r.ReadUInt32();
            var frames = new Matrix4x4[frameNum];

            for (var f = 0; f < (int)frameNum; f++)
            {
                frames[f] = GeometryObjectSerialization.RowMajor(r.ReadMatrix44Raw());
            }

            return new TextureUvAnimation { Subset = subset, Stage = stage, Frames = frames };
        }

        private static TextureImageAnimation ReadTextureImage(BinaryReader r, int subset, int stage)
        {
            var dataNum = r.ReadUInt32();
            var dataSeq = new TextureStage[dataNum];

            for (var d = 0; d < (int)dataNum; d++)
            {
                dataSeq[d] = r.ReadTexInfo();
            }

            return new TextureImageAnimation { Subset = subset, Stage = stage, DataSequence = dataSeq };
        }

        private static void WriteBoneBlock(BinaryWriter w, BoneAnimation bone, uint version)
        {
            if (version == 0)
            {
                w.Write(bone.Version);
            }

            w.WriteBoneAnimation(bone);
        }

        private static void WriteMatrixBlock(BinaryWriter w, MatrixAnimation matrix)
        {
            w.Write((uint)matrix.Frames.Length);
            foreach (var m in matrix.Frames)
            {
                w.WriteMatrix43(m);
            }
        }

        private static void WriteMaterialOpacity(BinaryWriter w, MaterialOpacityAnimation track)
        {
            w.Write((uint)track.Keys.Length);
            foreach (var k in track.Keys)
            {
                w.Write(k.Key);
                w.Write(k.SlerpType);
                w.Write(k.Value);
            }
        }

        private static void WriteTextureUv(BinaryWriter w, TextureUvAnimation track)
        {
            w.Write((uint)track.Frames.Length);
            foreach (var m in track.Frames)
            {
                w.WriteMatrix44(m);
            }
        }

        private static void WriteTextureImage(BinaryWriter w, TextureImageAnimation track)
        {
            w.Write((uint)track.DataSequence.Length);
            foreach (var stage in track.DataSequence)
            {
                w.WriteTexInfo(stage);
            }
        }
    }
}
