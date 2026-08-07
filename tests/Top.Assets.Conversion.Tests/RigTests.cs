using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Assets.Conversion.Models;
using Top.Assets.Conversion.Models.Gltf;
using Top.Gltf;
using Top.MindPower.Animation;
using Top.Tables.Custom;

namespace Top.Assets.Conversion.Tests
{
    public class RigTests
    {
        private static BoneAnimation MakeSkeleton()
        {
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

        private static CharacterAction Action(int no, int start, int end)
        {
            return new CharacterAction { ActionNo = no, StartFrame = start, EndFrame = end };
        }

        [Test]
        public void Actions_become_named_animations()
        {
            var file = GltfExport.Rig("rig", MakeSkeleton(), [Action(1, 0, 1), Action(60, 1, 1)]);

            Assert.That(file.Document.Animations.Select(a => a.Name),
                Is.EqualTo(new[] { "rig_01_waiting", "rig_60" }));
        }

        [Test]
        public void Clip_times_rebase_to_zero()
        {
            var file = GltfExport.Rig("rig", MakeSkeleton(), [Action(5, 1, 1)]);
            var data = new GltfData(new GltfFile
            {
                Document = file.Document,
                BinChunk = file.BinChunk,
            });

            var times = data.ReadFloats(file.Document.Animations[0].Samplers[0].Input);

            Assert.That(times, Is.EqualTo(new[] { 0f }));
        }

        [Test]
        public void Clips_sample_the_action_range()
        {
            // Child track position at frame 1 is (0, 0, 2); the axis swap
            // puts the translation in glTF y.
            var file = GltfExport.Rig("rig", MakeSkeleton(), [Action(5, 1, 1)]);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "translation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            Assert.That(values, Is.EqualTo(new[] { 0f, 2f, 0f }));
        }

        [Test]
        public void Single_position_quaternion_tracks_hold_the_position()
        {
            // Old-format .lab files store one position for non-root bones.
            var skeleton = MakeSkeleton();
            ((QuaternionBoneTrack)skeleton.Tracks[1]).Positions =
                [new Vector3(0f, 0f, 3f)];

            var file = GltfExport.Rig("rig", skeleton, [Action(1, 0, 1)]);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "translation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            Assert.That(values, Is.EqualTo(new[] { 0f, 3f, 0f, 0f, 3f, 0f }));
        }

        [Test]
        public void Quaternion_keys_are_normalized()
        {
            var skeleton = MakeSkeleton();
            var track = (QuaternionBoneTrack)skeleton.Tracks[1];
            track.Rotations =
            [
                new Quaternion(0f, 0f, 0f, 2f),
                new Quaternion(0f, 0f, 0f, 2f)
            ];

            var file = GltfExport.Rig("rig", skeleton, [Action(1, 0, 1)]);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "rotation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            Assert.That(values, Is.EqualTo(new[] { 0f, 0f, 0f, -1f, 0f, 0f, 0f, -1f }));
        }

        [Test]
        public void Clips_without_usable_tracks_are_skipped()
        {
            var skeleton = MakeSkeleton();
            skeleton.Tracks = null;

            var file = GltfExport.Rig("rig", skeleton, [Action(1, 0, 1)]);

            Assert.That(file.Document.Animations, Is.Null);
        }

        [Test]
        public void Matrix_fallback_bones_get_no_channels()
        {
            var skeleton = MakeSkeleton();
            var shear = Matrix4x4.Identity;
            shear.M21 = 0.5f;
            skeleton.Bones[1].InvBindMatrix = shear;

            var file = GltfExport.Rig("rig", skeleton, [Action(1, 0, 1)]);
            var doc = file.Document;

            Assert.That(doc.Nodes[1].Matrix, Is.Not.Null);
            Assert.That(doc.Animations[0].Channels.Any(c => c.Target.Node == 1), Is.False);
        }

        [Test]
        public void Matrix_tracks_decompose_to_channels()
        {
            var skeleton = MakeSkeleton();
            skeleton.KeyType = BoneKeyType.Mat43;
            skeleton.Tracks =
            [
                new MatrixBoneTrack
                {
                    Frames = [Matrix4x4.Identity, Matrix4x4.Identity],
                },
                new MatrixBoneTrack
                {
                    Frames =
                    [
                        Matrix4x4.CreateTranslation(0f, 0f, 1f),
                        Matrix4x4.CreateTranslation(0f, 0f, 2f)
                    ],
                }
            ];

            var file = GltfExport.Rig("rig", skeleton, [Action(1, 0, 1)]);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "translation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            Assert.That(values, Is.EqualTo(new[] { 0f, 1f, 0f, 0f, 2f, 0f }));
        }

        [Test]
        public void Bones_without_tracks_stay_in_bind_pose()
        {
            var skeleton = MakeSkeleton();
            skeleton.Tracks = [skeleton.Tracks[0]];

            var file = GltfExport.Rig("rig", skeleton, [Action(1, 0, 1)]);
            var doc = file.Document;

            Assert.That(doc.Animations[0].Channels.Any(c => c.Target.Node == 1), Is.False);
            Assert.That(doc.Animations[0].Channels.Any(c => c.Target.Node == 0), Is.True);
        }

        [Test]
        public void Tracks_shorter_than_the_clip_warn()
        {
            var skeleton = MakeSkeleton();
            var track = (QuaternionBoneTrack)skeleton.Tracks[1];
            track.Positions = [Vector3.Zero];
            track.Rotations = [Quaternion.Identity];

            var file = GltfExport.Rig("rig", skeleton, [Action(1, 0, 1)]);
            var doc = file.Document;

            Assert.That(doc.Animations[0].Channels.Any(c => c.Target.Node == 1), Is.False);
        }

        [Test]
        public void Quaternion_keys_keep_hemisphere_continuity()
        {
            var skeleton = MakeSkeleton();
            var track = (QuaternionBoneTrack)skeleton.Tracks[1];
            track.Rotations =
            [
                new Quaternion(0f, 0f, 0f, 1f),
                new Quaternion(0f, 0f, 0f, -1f)
            ];

            var file = GltfExport.Rig("rig", skeleton, [Action(1, 0, 1)]);
            var doc = file.Document;
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var channel = doc.Animations[0].Channels
                .First(c => c.Target.Node == 1 && c.Target.Path == "rotation");

            var values = data.ReadFloats(doc.Animations[0].Samplers[channel.Sampler].Output);

            // Both keys encode the same rotation; without the hemisphere fix
            // the second would flip sign and interpolate the long way round.
            Assert.That(values, Is.EqualTo(new[] { 0f, 0f, 0f, -1f, 0f, 0f, 0f, -1f }));
        }

        [Test]
        public void Skeleton_dummies_attach_to_bones()
        {
            var file = GltfExport.Rig("rig", MakeSkeleton(), [Action(1, 0, 1)]);
            var doc = file.Document;

            var dummy = doc.Nodes.FindIndex(n => n.Name == "dummy_9");

            Assert.That(dummy, Is.GreaterThanOrEqualTo(0));
            Assert.That(doc.Nodes[1].Children, Does.Contain(dummy));
            Assert.That(doc.Scenes[0].Nodes, Does.Not.Contain(dummy));

            // Bone 1's InvBindMatrix is T(0,0,-1) and the dummy's matrix is
            // T(1,0,0); the dummy's local transform under its bone is
            // T(1,0,0) * T(0,0,-1) = T(1,0,-1) in source space, which the
            // axis swap turns into glTF translation (1,-1,0). Emitting the
            // model-space matrix as the local would instead read (1,0,0) and
            // place the dummy at the bone's bind pose twice over.
            var matrix = doc.Nodes[dummy].Matrix;

            Assert.That(matrix, Is.Not.Null);
            Assert.That(new[] { matrix[12], matrix[13], matrix[14] },
                Is.EqualTo(new[] { 1f, -1f, 0f }));
        }

        [Test]
        public void Missing_action_table_falls_back_to_timeline()
        {
            var file = GltfExport.Rig("rig", MakeSkeleton(), null);

            Assert.That(file.Document.Animations.Single().Name, Is.EqualTo("rig_timeline"));
        }

        [Test]
        public void Out_of_range_clips_clamp()
        {
            var file = GltfExport.Rig("rig", MakeSkeleton(), [Action(1, 0, 9)]);
            var data = new GltfData(new GltfFile
            {
                Document = file.Document,
                BinChunk = file.BinChunk,
            });

            var times = data.ReadFloats(file.Document.Animations[0].Samplers[0].Input);

            Assert.That(times, Has.Length.EqualTo(2));
        }

        [Test]
        public void Converts_real_rig_with_action_table()
        {
            BoneAnimation skeleton;
            using (var stream = File.OpenRead(Fixtures.Path("lab/0001.lab")))
            {
                skeleton = LabFile.Read(stream).Animation;
            }

            CharacterActionTable table;
            using (var stream = File.OpenRead(Fixtures.Path("tables/CharacterAction.tx")))
            {
                table = CharacterActionTable.Read(stream);
            }

            // Skeleton 0001 belongs to action type 2 (characterinfo maps
            // framework 1 to Action ID 2).
            Assert.That(table.TryGetActions(2, out var actions), Is.True);

            var file = GltfExport.Rig("0001", skeleton, actions);
            var doc = file.Document;

            Assert.That(doc.Meshes, Is.Null, "empty arrays are invalid glTF");
            Assert.That(doc.Skins, Is.Null);
            // The shipped 0001.lab ends before some table ranges (late-added
            // actions); the mapper skips clips that start past the timeline.
            Assert.That(doc.Animations, Has.Count.EqualTo(
                actions.Count(a => a.StartFrame < skeleton.FrameCount)));

            var waiting = doc.Animations.First(a => a.Name == "0001_01_waiting");
            var data = new GltfData(new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var times = data.ReadFloats(waiting.Samplers[0].Input);

            Assert.That(times, Has.Length.EqualTo(49), "frames 0..48 inclusive");
        }

        [Test]
        public void Packaging_a_rig_writes_a_glb_and_no_textures()
        {
            var outputDir = Path.Combine(Path.GetTempPath(), "top-rig-tests");

            try
            {
                var file = GltfExport.Rig("rig", MakeSkeleton(), [Action(1, 0, 1)]);
                var result = new ModelPackaging(outputDir).Write(file, "rig");

                Assert.That(result.ModelPath, Does.EndWith("rig.glb"));
                Assert.That(File.Exists(result.ModelPath), Is.True);
                Assert.That(result.TexturePaths, Is.Empty);
            }
            finally
            {
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, recursive: true);
                }
            }
        }
    }
}
