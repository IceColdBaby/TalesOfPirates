using System.Linq;
using NUnit.Framework;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Gltf.Tests
{
    public class GltfObjectHostingTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            var builder = new GltfBuilder("hosting");
            var skel = builder.AddNode("skel");
            var b0 = builder.AddNode("b0");
            var b1 = builder.AddNode("b1");
            var other = builder.AddNode("other");
            var c0 = builder.AddNode("c0");

            skel.AddChild(b0).AddChild(c0).AsRoot();
            b0.AddChild(b1);
            other.AsRoot();

            var turn = TestGltf.QuarterTurn(builder);

            Clip(builder, "subtree", turn, b0, b1);
            Clip(builder, "spread", turn, b0, other);
            Clip(builder, "siblings", turn, b1, c0);

            _root = GltfObjectBuilder.Build(new GltfData(builder.Build()), "hosting",
                System.Array.Empty<Material>(), null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        private static void Clip(GltfBuilder builder, string name, (int Input, int Output) turn,
            GltfNodeRef first, GltfNodeRef second)
        {
            builder.AddAnimation(name)
                .AddChannel(turn.Input, turn.Output, first, "rotation")
                .AddChannel(turn.Input, turn.Output, second, "rotation");
        }

        private string[] Paths(string clipName)
        {
            var clip = _root.GetComponentsInChildren<Animation>(true)
                .Select(animation => animation.GetClip(clipName))
                .First(candidate => candidate != null);

            return AnimationUtility.GetCurveBindings(clip)
                .Select(b => b.path).Distinct().OrderBy(p => p).ToArray();
        }

        [Test]
        public void Subtree_clip_hosts_on_its_root_target()
        {
            var b0 = _root.transform.Find("skel/b0");

            Assert.That(b0.GetComponent<Animation>(), Is.Not.Null);
            Assert.That(b0.GetComponent<Animation>().clip.name, Is.EqualTo("subtree"));
            Assert.That(_root.transform.Find("skel").GetComponent<Animation>()
                .GetClip("subtree"), Is.Null);
        }

        [Test]
        public void Subtree_clip_paths_are_relative_to_the_host()
        {
            Assert.That(Paths("subtree"), Is.EqualTo(new[] { string.Empty, "b1" }));
        }

        [Test]
        public void Clip_without_a_common_ancestor_hosts_on_the_model_root()
        {
            Assert.That(_root.GetComponent<Animation>(), Is.Not.Null);
            Assert.That(Paths("spread"), Is.EqualTo(new[] { "other", "skel/b0" }));
        }

        [Test]
        public void Sibling_targets_host_on_their_shared_parent()
        {
            var skel = _root.transform.Find("skel");

            Assert.That(skel.GetComponent<Animation>(), Is.Not.Null);
            Assert.That(skel.GetComponent<Animation>().clip.name, Is.EqualTo("siblings"));
            Assert.That(Paths("siblings"), Is.EqualTo(new[] { "b0/b1", "c0" }));
        }
    }
}
