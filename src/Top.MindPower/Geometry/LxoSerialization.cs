using System.IO;
using System.Numerics;
using Top.MindPower.Animation;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// lwModelNodeInfo::Load/Save (lwExpObj.cpp).
    /// </summary>
    internal static class LxoSerialization
    {
        private const int DescriptorBytes = 64;

        internal static SceneNode ReadSceneNode(this BinaryReader r, uint version)
        {
            var handle = r.ReadUInt32();
            var type = (SceneNodeType)r.ReadUInt32();
            var id = r.ReadUInt32();
            var name = r.ReadFixedString(DescriptorBytes);
            var parentHandle = r.ReadUInt32();
            var linkParentId = r.ReadUInt32();
            var linkId = r.ReadUInt32();

            var data = type switch
            {
                SceneNodeType.Primitive => (object)r.ReadGeometryObject(version),
                SceneNodeType.BoneCtrl => ReadBoneCtrl(r, version),
                SceneNodeType.Dummy => ReadSceneDummy(r),
                SceneNodeType.Helper => r.ReadHelper(version),
                _ => throw new ParseException("lxo", version, r.BaseStream.Position,
                    $"unsupported scene node type {(uint)type}"),
            };

            return new SceneNode
            {
                Type = type,
                Handle = handle,
                Id = id,
                Name = name,
                ParentHandle = parentHandle,
                LinkParentId = linkParentId,
                LinkId = linkId,
                Data = data,
            };
        }

        internal static void WriteSceneNode(this BinaryWriter w, SceneNode node, uint version)
        {
            w.Write(node.Handle);
            w.Write((uint)node.Type);
            w.Write(node.Id);
            w.WriteFixedString(node.Name, DescriptorBytes);
            w.Write(node.ParentHandle);
            w.Write(node.LinkParentId);
            w.Write(node.LinkId);

            switch (node.Type)
            {
                case SceneNodeType.Primitive:
                    w.WriteGeometryObject((GeometryObject)node.Data);
                    break;
                case SceneNodeType.BoneCtrl:
                    WriteBoneCtrl(w, (BoneAnimation)node.Data, version);
                    break;
                case SceneNodeType.Dummy:
                    WriteSceneDummy(w, (SceneDummy)node.Data);
                    break;
                case SceneNodeType.Helper:
                    w.WriteHelper((Helper)node.Data, version);
                    break;
                default:
                    throw new ParseException("lxo", version, w.BaseStream.Position,
                        $"unsupported scene node type {(uint)node.Type}");
            }
        }

        private static BoneAnimation ReadBoneCtrl(BinaryReader r, uint version)
        {
            var boneVersion = version;
            if (boneVersion == 0)
            {
                boneVersion = r.ReadUInt32();
            }

            return r.ReadBoneAnimation(boneVersion);
        }

        private static void WriteBoneCtrl(BinaryWriter w, BoneAnimation bone, uint version)
        {
            if (version == 0)
            {
                w.Write(bone.Version);
            }

            w.WriteBoneAnimation(bone);
        }

        private static SceneDummy ReadSceneDummy(BinaryReader r)
        {
            var dummy = new SceneDummy
            {
                Id = r.ReadUInt32(),
                Local = GeometryObjectSerialization.RowMajor(r.ReadMatrix44Raw()),
            };

            if (r.ReadUInt32() == 1)
            {
                dummy.Animation = ReadMatrixAnimation(r);
            }

            return dummy;
        }

        private static void WriteSceneDummy(BinaryWriter w, SceneDummy dummy)
        {
            w.Write(dummy.Id);
            w.WriteMatrix44(dummy.Local);
            w.Write(dummy.Animation != null ? 1u : 0u);

            if (dummy.Animation != null)
            {
                w.Write((uint)dummy.Animation.Frames.Length);
                foreach (var frame in dummy.Animation.Frames)
                {
                    w.WriteMatrix43(frame);
                }
            }
        }

        private static MatrixAnimation ReadMatrixAnimation(BinaryReader r)
        {
            var frameNum = r.ReadUInt32();
            var frames = new Matrix4x4[frameNum];

            for (var f = 0; f < (int)frameNum; f++)
            {
                frames[f] = r.ReadMatrix43();
            }

            return new MatrixAnimation { Frames = frames };
        }
    }
}
