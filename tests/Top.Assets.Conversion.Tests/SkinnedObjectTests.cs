using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Assets.Conversion.Models.Gltf;
using Top.Gltf;
using Top.MindPower.Animation;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Tests
{
    public class SkinnedObjectTests
    {
        private static BoneAnimation MakeSkeleton()
        {
            // Tracks and dummies are present to prove parts ignore them.
            return new BoneAnimation
            {
                FrameCount = 2,
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
                        Positions = [Vector3.Zero, Vector3.Zero],
                        Rotations = [Quaternion.Identity, Quaternion.Identity],
                    },
                    new QuaternionBoneTrack
                    {
                        Positions = [new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 2f)],
                        Rotations = [Quaternion.Identity, Quaternion.Identity],
                    }
                ],
            };
        }

        private static GeometryObject MakeSkinnedObject()
        {
            return new GeometryObject
            {
                Id = 3,
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
                        new SkinBlend { BoneIndex = 0x00000100, Weight0 = 0.75f, Weight1 = 0.25f },
                        new SkinBlend { BoneIndex = 0x00000000, Weight0 = 1f },
                        new SkinBlend { BoneIndex = 0x00000001, Weight0 = 1f }
                    ],
                    BoneIndices = [0, 1],
                },
            };
        }

        [Test]
        public void Bones_become_a_node_hierarchy_with_a_skin()
        {
            var file = GltfExport.Object("rig", MakeSkinnedObject(), MakeSkeleton());
            var doc = file.Document;

            Assert.That(doc.Nodes[0].Name, Is.EqualTo("root"));
            Assert.That(doc.Nodes[1].Name, Is.EqualTo("child"));
            Assert.That(doc.Nodes[0].Children, Does.Contain(1));
            Assert.That(doc.Scenes[0].Nodes, Does.Contain(0));
            Assert.That(doc.Scenes[0].Nodes, Does.Not.Contain(1));

            Assert.That(doc.Skins, Has.Count.EqualTo(1));
            Assert.That(doc.Skins[0].Joints, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(doc.Skins[0].Skeleton, Is.EqualTo(0));

            var meshNode = doc.Nodes.Find(n => n.Name == "geom_3");
            Assert.That(meshNode.Skin, Is.EqualTo(0));
        }

        [Test]
        public void Bind_pose_becomes_node_transforms()
        {
            var doc = GltfExport.Object("rig", MakeSkinnedObject(), MakeSkeleton()).Document;

            Assert.That(doc.Nodes[0].Matrix, Is.Null, "identity bind pose needs no transform");
            Assert.That(doc.Nodes[0].Translation, Is.Null);

            // Child bind world = InvBind^-1 = T(0,0,1); parent is identity, so
            // the local matches, and the axis swap puts the translation in
            // glTF y. Bones carry TRS so clips can target them.
            Assert.That(doc.Nodes[1].Matrix, Is.Null);
            Assert.That(doc.Nodes[1].Translation, Is.EqualTo(new[] { 0f, 1f, 0f }));
            Assert.That(doc.Nodes[1].Rotation, Is.EqualTo(new[] { 0f, 0f, 0f, 1f }));
            Assert.That(doc.Nodes[1].Scale, Is.EqualTo(new[] { 1f, 1f, 1f }));
        }

        [Test]
        public void Blend_data_maps_to_joints_and_weights()
        {
            var file = GltfExport.Object("rig", MakeSkinnedObject(), MakeSkeleton());
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var primitive = doc.Meshes.First(m => m.Name == "geom_3").Primitives[0];

            var joints = data.ReadInts(primitive.Attributes["JOINTS_0"]);
            var weights = data.ReadFloats(primitive.Attributes["WEIGHTS_0"]);

            Assert.That(joints, Is.EqualTo(new[] { 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 }));
            Assert.That(weights, Is.EqualTo(new[]
            {
                0.75f, 0.25f, 0f, 0f,
                1f, 0f, 0f, 0f,
                1f, 0f, 0f, 0f,
            }));
        }

        [Test]
        public void Inverse_bind_matrices_swap_axes()
        {
            var file = GltfExport.Object("rig", MakeSkinnedObject(), MakeSkeleton());
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });

            var floats = data.ReadFloats(doc.Skins[0].InverseBindMatrices.Value);

            Assert.That(floats, Has.Length.EqualTo(32));
            // Second joint: ToP translation (0, 0, -1) lands in glTF y.
            Assert.That(floats[16 + 12], Is.EqualTo(0f));
            Assert.That(floats[16 + 13], Is.EqualTo(-1f));
            Assert.That(floats[16 + 14], Is.EqualTo(0f));
        }

        [Test]
        public void Parts_carry_no_animation_and_no_skeleton_dummies()
        {
            var doc = GltfExport.Object("rig", MakeSkinnedObject(), MakeSkeleton()).Document;

            Assert.That(doc.Animations, Is.Null);
            Assert.That(doc.Nodes.Exists(n => n.Name.StartsWith("dummy_")), Is.False);
        }

        [Test]
        public void Converts_real_character_part_with_skeleton()
        {
            BoneAnimation skeleton;
            using (var stream = File.OpenRead(Fixtures.Path("lab/0724.lab")))
            {
                skeleton = LabFile.Read(stream).Animation;
            }

            GeometryObject obj;
            using (var stream = File.OpenRead(Fixtures.Path("lgo/0724000000.lgo")))
            {
                obj = LgoFile.Read(stream).Object;
            }

            var file = GltfExport.Object("0724000000", obj, skeleton);
            var doc = file.Document;

            // The shipped part carries four NaN normals.
            Assert.That(doc.Nodes[0].Name, Is.EqualTo("Dummy01"));
            Assert.That(doc.Skins[0].Joints, Has.Count.EqualTo(1));
            Assert.That(doc.Animations, Is.Null, "parts are silent");
            Assert.That(doc.Nodes.Exists(n => n.Name.StartsWith("dummy_")), Is.False,
                "skeleton dummies belong to the rig asset");
        }
    }
}
