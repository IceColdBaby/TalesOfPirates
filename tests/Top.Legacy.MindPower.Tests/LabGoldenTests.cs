using System.IO;
using System.Numerics;
using NUnit.Framework;
using Top.Legacy.MindPower.Animation;

namespace Top.Legacy.MindPower.Tests
{
    public class LabGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // 0724.lab: EXP_OBJ_VERSION_1_0_0_5 (0x1005), 1 bone, 12 frames, 6 dummies, Quat keys.
            // lwExpObj.cpp: version>=EXP_OBJ_VERSION_1_0_0_3 => all bones get frameNum positions.
            using var stream = File.OpenRead(Fixtures.Path("lab/0724.lab"));
            var animation = LabFile.Read(stream).Animation;

            Assert.That(animation.Version, Is.EqualTo(0x1005u));
            Assert.That(animation.KeyType, Is.EqualTo(BoneKeyType.Quat));
            Assert.That(animation.FrameCount, Is.EqualTo(12));
            Assert.That(animation.Bones.Length, Is.EqualTo(1));
            Assert.That(animation.Dummies.Length, Is.EqualTo(6));

            // Bone 0: lwBoneBaseInfo name="Dummy01", id=0, parentId=-1 (LW_INVALID_INDEX, root).
            Assert.That(animation.Bones[0].Name, Is.EqualTo("Dummy01"));
            Assert.That(animation.Bones[0].Id, Is.EqualTo(0));
            Assert.That(animation.Bones[0].ParentId, Is.EqualTo(-1));

            // Quat track for bone 0: frameNum positions (version>=0x1003) + frameNum quats.
            var track0 = (QuaternionBoneTrack)animation.Tracks[0];
            Assert.That(track0.Positions.Length, Is.EqualTo(12));
            Assert.That(track0.Rotations.Length, Is.EqualTo(12));

            // First frame position and rotation.
            Assert.That(track0.Positions[0].X, Is.EqualTo(0f).Within(1e-6f));
            // Y skipped: stored as -0f, which compares equal to 0f but differs at the bit level.
            Assert.That(track0.Positions[0].Z, Is.EqualTo(0.7229491f).Within(1e-6f));
            Assert.That(track0.Rotations[0], Is.EqualTo(Quaternion.Identity));

            // Last frame (11) matches first (static animation for this bone).
            Assert.That(track0.Positions[11], Is.EqualTo(track0.Positions[0]));
            Assert.That(track0.Rotations[11], Is.EqualTo(track0.Rotations[0]));
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("lab/0724.lab", LabFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("lab", "*.lab",
                LabFile.Read, (s, f) => f.Write(s));
        }
    }
}
