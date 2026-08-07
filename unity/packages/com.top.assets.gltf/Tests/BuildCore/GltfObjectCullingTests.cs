using NUnit.Framework;
using Top.Gltf;
using UnityEngine;

namespace Top.Assets.Gltf.Tests
{
    public class GltfObjectCullingTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            var builder = new GltfBuilder("culling");
            var mesh = TestGltf.Triangle(builder, "mesh", "mat");

            builder.AddNode("mesh_sibling").WithMesh(mesh).AsRoot();
            var skel = builder.AddNode("skel").AsRoot();
            var bone = builder.AddNode("bone1");
            skel.AddChild(bone);
            var renderedParent = builder.AddNode("rendered_parent").WithMesh(mesh).AsRoot();
            var renderedChild = builder.AddNode("rendered_child");
            renderedParent.AddChild(renderedChild);

            var turn = TestGltf.QuarterTurn(builder);

            builder.AddAnimation("bone_clip")
                .AddChannel(turn.Input, turn.Output, skel, "rotation")
                .AddChannel(turn.Input, turn.Output, bone, "rotation");
            builder.AddAnimation("rendered_clip")
                .AddChannel(turn.Input, turn.Output, renderedParent, "rotation")
                .AddChannel(turn.Input, turn.Output, renderedChild, "rotation");

            _root = GltfObjectBuilder.Build(new GltfData(builder.Build()), "culling",
                new Material[] { null }, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Renderer_less_host_gets_always_animate()
        {
            var skel = _root.transform.Find("skel");
            var animation = skel.GetComponent<Animation>();

            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.clip.name, Is.EqualTo("bone_clip"));
            Assert.That(skel.GetComponentInChildren<Renderer>(true), Is.Null,
                "the sibling mesh node must not count as being under the skeleton host");
            Assert.That(animation.cullingType, Is.EqualTo(AnimationCullingType.AlwaysAnimate));
        }

        [Test]
        public void Host_with_a_renderer_in_its_own_hierarchy_keeps_based_on_renderers()
        {
            var renderedParent = _root.transform.Find("rendered_parent");
            var animation = renderedParent.GetComponent<Animation>();

            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.clip.name, Is.EqualTo("rendered_clip"));
            Assert.That(animation.cullingType, Is.EqualTo(AnimationCullingType.BasedOnRenderers));
        }
    }
}
