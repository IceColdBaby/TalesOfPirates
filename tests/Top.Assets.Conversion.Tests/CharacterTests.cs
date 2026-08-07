using System;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Assets.Conversion.Models;
using Top.Assets.Conversion.Models.Gltf;
using Top.Gltf;
using Top.MindPower.Animation;
using Top.MindPower.Geometry;
using Top.Tables.Custom;

namespace Top.Assets.Conversion.Tests
{
    public class CharacterTests
    {
        private static BoneAnimation MakeSkeleton()
        {
            return new BoneAnimation
            {
                FrameCount = 4,
                KeyType = BoneKeyType.Quat,
                Bones =
                [
                    new Bone { Name = "root", Id = 0, ParentId = -1, InvBindMatrix = Matrix4x4.Identity },
                    new Bone
                    {
                        Name = "child", Id = 1, ParentId = 0,
                        InvBindMatrix = Matrix4x4.CreateTranslation(0f, 0f, -1f),
                    }
                ],
                Dummies =
                [
                    new BoneDummy
                    {
                        Id = 9, ParentBoneId = 1,
                        Matrix = Matrix4x4.CreateTranslation(1f, 0f, 0f),
                    }
                ],
                Tracks =
                [
                    new QuaternionBoneTrack
                    {
                        Positions = [Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero],
                        Rotations =
                        [
                            Quaternion.Identity, Quaternion.Identity,
                            Quaternion.Identity, Quaternion.Identity
                        ],
                    },
                    new QuaternionBoneTrack
                    {
                        Positions =
                        [
                            new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 2f),
                            new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 4f)
                        ],
                        Rotations =
                        [
                            Quaternion.Identity, Quaternion.Identity,
                            Quaternion.Identity, Quaternion.Identity
                        ],
                    }
                ],
            };
        }

        private static GeometryObject MakePart(uint id, uint[] boneIndices)
        {
            return new GeometryObject
            {
                Id = id,
                ParentId = uint.MaxValue,
                LocalMatrix = Matrix4x4.Identity,
                Mesh = new Mesh
                {
                    PointType = 4,
                    Vertices =
                    [
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(0, 1, 0)
                    ],
                    Indices = [0, 1, 2],
                    Subsets = [new MeshSubset { PrimitiveCount = 1, StartIndex = 0 }],
                    SkinBlends =
                    [
                        new SkinBlend { BoneIndex = 0x00000000, Weight0 = 1f },
                        new SkinBlend { BoneIndex = 0x00000001, Weight0 = 1f },
                        new SkinBlend { BoneIndex = 0x00000001, Weight0 = 1f }
                    ],
                    BoneIndices = boneIndices,
                },
            };
        }

        private static CharacterAction[] MakeActions()
        {
            return
            [
                new CharacterAction { ActionNo = 1, StartFrame = 0, EndFrame = 1 },
                new CharacterAction { ActionNo = 5, StartFrame = 2, EndFrame = 3 }
            ];
        }

        [Test]
        public void Parts_share_one_skin_on_one_skeleton()
        {
            var parts = new[] { MakePart(0, [0, 1]), MakePart(2, [1, 0]) };
            var file = GltfExport.Character("0055", MakeSkeleton(), parts, MakeActions());
            var doc = file.Document;

            Assert.That(doc.Skins, Has.Count.EqualTo(1));

            var first = doc.Nodes.Find(n => n.Name == "geom_0");
            var second = doc.Nodes.Find(n => n.Name == "geom_2");

            Assert.That(first.Skin, Is.EqualTo(0));
            Assert.That(second.Skin, Is.EqualTo(0));
        }

        [Test]
        public void Joints_are_remapped_into_skeleton_bone_space_per_part()
        {
            var parts = new[] { MakePart(0, [0, 1]), MakePart(2, [1, 0]) };
            var file = GltfExport.Character("0055", MakeSkeleton(), parts, MakeActions());
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });

            var straight = doc.Meshes.First(m => m.Name == "geom_0").Primitives[0];
            var swapped = doc.Meshes.First(m => m.Name == "geom_2").Primitives[0];

            Assert.That(data.ReadInts(straight.Attributes["JOINTS_0"]),
                Is.EqualTo(new[] { 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 }));
            Assert.That(data.ReadInts(swapped.Attributes["JOINTS_0"]),
                Is.EqualTo(new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
        }

        [Test]
        public void Dummies_and_action_clips_are_emitted()
        {
            var parts = new[] { MakePart(0, [0, 1]) };
            var doc = GltfExport.Character("0055", MakeSkeleton(), parts, MakeActions()).Document;

            Assert.That(doc.Nodes.Exists(n => n.Name == "dummy_9"), Is.True);
            Assert.That(doc.Animations.Select(a => a.Name),
                Is.EqualTo(new[] { "0055_01_waiting", "0055_05_run" }));
        }

        [Test]
        public void Missing_actions_fall_back_to_the_full_timeline()
        {
            var parts = new[] { MakePart(0, [0, 1]) };
            var file = GltfExport.Character("0055", MakeSkeleton(), parts, null);

            Assert.That(file.Document.Animations.Select(a => a.Name),
                Is.EqualTo(new[] { "0055_timeline" }));
        }

        [Test]
        public void Part_without_blend_data_stays_rigid_with_a_warning()
        {
            var rigid = MakePart(1, [0, 1]);
            rigid.Mesh.SkinBlends = null;
            var parts = new[] { MakePart(0, [0, 1]), rigid };

            var file = GltfExport.Character("0055", MakeSkeleton(), parts, MakeActions());
            var doc = file.Document;

            Assert.That(doc.Nodes.Find(n => n.Name == "geom_1").Skin, Is.Null);
            Assert.That(doc.Skins, Has.Count.EqualTo(1));
        }

        [Test]
        public void Packaging_a_character_writes_the_glb()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var parts = new[] { MakePart(0, [0, 1]) };

            try
            {
                var packaging = new ModelPackaging(
                    Path.Combine(dir, "model"), Path.Combine(dir, "textures"));

                var file = GltfExport.Character("0055", MakeSkeleton(), parts, MakeActions(), packaging.TextureUriPrefix);

                var result = packaging.Write(file, parts, "0055", Path.Combine(dir, "textures-src"));

                Assert.That(File.Exists(Path.Combine(dir, "model", "0055.glb")), Is.True);
                Assert.That(result.ModelPath, Is.EqualTo(Path.Combine(dir, "model", "0055.glb")));
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
        }

        [Test]
        public void Clipless_map_emits_no_animations_and_no_fallback_warning()
        {
            var parts = new[] { MakePart(0, [0, 1]) };
            var file = GltfExport.Character("0055", MakeSkeleton(), parts, null, emitClips: false);

            Assert.That(file.Document.Animations, Is.Null);
            Assert.That(file.Document.Skins, Has.Count.EqualTo(1));
        }
    }
}
