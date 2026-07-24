using System.Collections.Generic;
using Top.Gltf;
using Top.MindPower.Animation;
using Top.MindPower.Geometry;
using Top.Tables.Custom;

namespace Top.Assets.Conversion
{
    /// <summary>
    /// Composes a complete character document: the .lab skeleton with its
    /// dummies, every .lgo part as a skinned mesh sharing one skin over
    /// the skeleton's joints, and one animation per character action -
    /// the union of what RigGltfMapper and the single-part skinned path
    /// emit separately. Parts without blend data stay rigid so the
    /// problem is visible in the imported model. Without an action table
    /// entry the whole timeline becomes one clip. Clips can be omitted for
    /// bakes that share a rig's clips.
    /// </summary>
    public static class CharacterGltfMapper
    {
        public static GltfConversion Map(BoneAnimation skeleton, GeometryObject[] parts,
            CharacterAction[] actions, string name, string textureUriPrefix = null,
            bool emitClips = true)
        {
            var b = new GltfBuilder(name, GltfMapping.Generator);
            var warnings = new List<string>();
            var boneNodes = GltfMapping.AddSkeleton(b, skeleton, warnings);

            GltfMapping.AddDummies(b, skeleton, boneNodes, warnings);

            GltfSkinRef skin = null;

            foreach (var part in parts)
            {
                var skinned = GeometryObjectGltfMapper.HasBlendData(part.Mesh);
                int? jointsAccessor = null;
                int? weightsAccessor = null;

                if (skinned)
                {
                    (jointsAccessor, weightsAccessor) =
                        GeometryObjectGltfMapper.AddSkinAttributes(b.Buffer, part.Mesh);
                }
                else
                {
                    warnings.Add($"part {part.Id} carries no blend data; mesh stays rigid");
                }

                var nodeName = GltfContract.Geometry(part.Id);
                var (meshRef, _) = GeometryObjectGltfMapper.AddGeometryMesh(b, part, nodeName,
                    name, textureUriPrefix, litSubset: null, warnings, jointsAccessor,
                    weightsAccessor);
                var node = b.AddNode(nodeName).WithMesh(meshRef);

                if (skinned)
                {
                    skin ??= GeometryObjectGltfMapper.AddSkin(b, skeleton, boneNodes, name);
                    node.WithSkin(skin);
                }
                else if (!part.LocalMatrix.IsIdentity)
                {
                    node.WithMatrix(GltfMapping.ToGltfMatrix(GltfMapping.ToGltf(part.LocalMatrix)));
                }

                node.AsRoot();
            }

            if (emitClips)
            {
                if (actions == null || actions.Length == 0)
                {
                    actions = new[]
                    {
                        new CharacterAction { StartFrame = 0, EndFrame = skeleton.FrameCount - 1 },
                    };
                    warnings.Add("no action table entry; emitting the full timeline as one clip");
                }

                var tracks = BoneTracks.Convert(skeleton, warnings);

                foreach (var action in actions)
                {
                    BoneTracks.AddAnimation(b, skeleton, boneNodes, tracks,
                        action.StartFrame, action.EndFrame,
                        GltfContract.ActionClip(name, action.ActionNo), warnings);
                }
            }

            return GeometryObjectGltfMapper.Finish(b, warnings);
        }
    }
}
