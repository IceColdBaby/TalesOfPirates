using System.Collections.Generic;
using Top.Gltf;
using Top.MindPower.Animation;
using Top.Tables.Custom;

namespace Top.Assets.Conversion
{
    /// <summary>
    /// Projects a .lab skeleton into a mesh-less rig glTF: the bind-pose
    /// bone hierarchy, skeleton dummies as locator nodes parented to their
    /// bones, and one animation per character action (frame sub-ranges of
    /// the single .lab timeline, rebased to start at zero). Parts converted
    /// by GeometryObjectGltfMapper stay silent; all motion lives here.
    /// Without an action table entry the whole timeline becomes one clip.
    /// </summary>
    public static class RigGltfMapper
    {
        public static GltfConversion Map(BoneAnimation skeleton, CharacterAction[] actions, string name)
        {
            var b = new GltfBuilder(name, GltfMapping.Generator);
            var warnings = new List<string>();
            var boneNodes = GltfMapping.AddSkeleton(b, skeleton, warnings);

            GltfMapping.AddDummies(b, skeleton, boneNodes, warnings);

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
                    action.StartFrame, action.EndFrame, GltfContract.ActionClip(name, action.ActionNo), warnings);
            }

            var file = b.Build();

            return new GltfConversion
            {
                Document = file.Document,
                Bin = file.BinChunk,
                Warnings = warnings,
            };
        }
    }
}
