using System;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Gltf;
using Top.MindPower.Animation;
using Top.MindPower.Geometry;
using Top.Tables.Custom;

namespace Top.Assets.Conversion.Tests
{
    public class CharacterMapperTests
    {
        private static BoneAnimation MakeSkeleton()
        {
            return new BoneAnimation
            {
                FrameCount = 4,
                KeyType = BoneKeyType.Quat,
                Bones = new[]
                {
                    new Bone { Name = "root", Id = 0, ParentId = -1, InvBindMatrix = Matrix4x4.Identity },
                    new Bone
                    {
                        Name = "child", Id = 1, ParentId = 0,
                        InvBindMatrix = Matrix4x4.CreateTranslation(0f, 0f, -1f),
                    },
                },
                Dummies = new[]
                {
                    new BoneDummy
                    {
                        Id = 9, ParentBoneId = 1,
                        Matrix = Matrix4x4.CreateTranslation(1f, 0f, 0f),
                    },
                },
                Tracks = new BoneTrack[]
                {
                    new QuaternionBoneTrack
                    {
                        Positions = new[] { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero },
                        Rotations = new[]
                        {
                            Quaternion.Identity, Quaternion.Identity,
                            Quaternion.Identity, Quaternion.Identity,
                        },
                    },
                    new QuaternionBoneTrack
                    {
                        Positions = new[]
                        {
                            new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 2f),
                            new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 4f),
                        },
                        Rotations = new[]
                        {
                            Quaternion.Identity, Quaternion.Identity,
                            Quaternion.Identity, Quaternion.Identity,
                        },
                    },
                },
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
                    Vertices = new[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(0, 1, 0),
                    },
                    Indices = new uint[] { 0, 1, 2 },
                    Subsets = new[] { new MeshSubset { PrimitiveCount = 1, StartIndex = 0 } },
                    SkinBlends = new[]
                    {
                        new SkinBlend { BoneIndex = 0x00000000, Weight0 = 1f },
                        new SkinBlend { BoneIndex = 0x00000001, Weight0 = 1f },
                        new SkinBlend { BoneIndex = 0x00000001, Weight0 = 1f },
                    },
                    BoneIndices = boneIndices,
                },
            };
        }

        private static CharacterAction[] MakeActions()
        {
            return new[]
            {
                new CharacterAction { ActionNo = 1, StartFrame = 0, EndFrame = 1 },
                new CharacterAction { ActionNo = 5, StartFrame = 2, EndFrame = 3 },
            };
        }

        [Test]
        public void Parts_share_one_skin_on_one_skeleton()
        {
            var parts = new[] { MakePart(0, new uint[] { 0, 1 }), MakePart(2, new uint[] { 1, 0 }) };
            var conversion = CharacterGltfMapper.Map(MakeSkeleton(), parts, MakeActions(), "0055");
            var doc = conversion.Document;

            Assert.That(conversion.Warnings, Is.Empty);
            Assert.That(doc.Skins, Has.Count.EqualTo(1));

            var first = doc.Nodes.Find(n => n.Name == "geom_0");
            var second = doc.Nodes.Find(n => n.Name == "geom_2");

            Assert.That(first.Skin, Is.EqualTo(0));
            Assert.That(second.Skin, Is.EqualTo(0));
        }

        [Test]
        public void Joints_are_remapped_into_skeleton_bone_space_per_part()
        {
            var parts = new[] { MakePart(0, new uint[] { 0, 1 }), MakePart(2, new uint[] { 1, 0 }) };
            var conversion = CharacterGltfMapper.Map(MakeSkeleton(), parts, MakeActions(), "0055");
            var doc = conversion.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = conversion.Bin });

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
            var parts = new[] { MakePart(0, new uint[] { 0, 1 }) };
            var doc = CharacterGltfMapper.Map(MakeSkeleton(), parts, MakeActions(), "0055").Document;

            Assert.That(doc.Nodes.Exists(n => n.Name == "dummy_9"), Is.True);
            Assert.That(doc.Animations.Select(a => a.Name),
                Is.EqualTo(new[] { "0055_01_waiting", "0055_05_run" }));
        }

        [Test]
        public void Missing_actions_fall_back_to_the_full_timeline()
        {
            var parts = new[] { MakePart(0, new uint[] { 0, 1 }) };
            var conversion = CharacterGltfMapper.Map(MakeSkeleton(), parts, null, "0055");

            Assert.That(conversion.Warnings,
                Does.Contain("no action table entry; emitting the full timeline as one clip"));
            Assert.That(conversion.Document.Animations.Select(a => a.Name),
                Is.EqualTo(new[] { "0055_timeline" }));
        }

        [Test]
        public void Part_without_blend_data_stays_rigid_with_a_warning()
        {
            var rigid = MakePart(1, new uint[] { 0, 1 });
            rigid.Mesh.SkinBlends = null;
            var parts = new[] { MakePart(0, new uint[] { 0, 1 }), rigid };

            var conversion = CharacterGltfMapper.Map(MakeSkeleton(), parts, MakeActions(), "0055");
            var doc = conversion.Document;

            Assert.That(conversion.Warnings,
                Does.Contain("part 1 carries no blend data; mesh stays rigid"));
            Assert.That(doc.Nodes.Find(n => n.Name == "geom_1").Skin, Is.Null);
            Assert.That(doc.Skins, Has.Count.EqualTo(1));
        }

        [Test]
        public void ConvertCharacter_writes_the_glb_and_reports_warnings()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var parts = new[] { MakePart(0, new uint[] { 0, 1 }) };

            try
            {
                var result = ModelConversion.ConvertCharacter(MakeSkeleton(), parts,
                    MakeActions(), "0055", Path.Combine(dir, "textures-src"),
                    Path.Combine(dir, "model"), Path.Combine(dir, "textures"));

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
            var parts = new[] { MakePart(0, new uint[] { 0, 1 }) };
            var conversion = CharacterGltfMapper.Map(MakeSkeleton(), parts, null, "0055",
                emitClips: false);

            Assert.That(conversion.Document.Animations, Is.Null);
            Assert.That(conversion.Warnings, Is.Empty);
            Assert.That(conversion.Document.Skins, Has.Count.EqualTo(1));
        }
    }
}
