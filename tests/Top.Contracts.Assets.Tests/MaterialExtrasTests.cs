using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Top.Contracts.Assets.Models.Extras;
using Top.Contracts.Assets.Models.Materials;

namespace Top.Contracts.Assets.Tests
{
    public class MaterialExtrasTests
    {
        private static JObject Carrying(MaterialExtras extras)
        {
            return new JObject { [MaterialExtras.Key] = extras.ToJson() };
        }

        [Test]
        public void A_material_carrying_no_extras_reads_as_absent()
        {
            Assert.That(MaterialExtras.TryRead(null, out var extras), Is.False);
            Assert.That(extras, Is.Null);
        }

        [Test]
        public void Extras_from_another_tool_are_not_a_TOP_material()
        {
            Assert.That(MaterialExtras.TryRead(new JObject { ["blender"] = "custom" }, out _), Is.False);
        }

        [Test]
        public void An_unreadable_payload_reads_as_absent_rather_than_throwing()
        {
            var extras = new JObject { [MaterialExtras.Key] = "not an object" };

            Assert.That(MaterialExtras.TryRead(extras, out _), Is.False);
        }

        [Test]
        public void An_empty_payload_leaves_every_section_absent()
        {
            var container = new JObject { [MaterialExtras.Key] = new JObject() };

            Assert.That(MaterialExtras.TryRead(container, out var extras), Is.True);
            Assert.That(extras.RenderState, Is.Null);
            Assert.That(extras.UvAnimation, Is.Null);
            Assert.That(extras.OpacityAnimation, Is.Null);
            Assert.That(extras.Flipbook, Is.Null);
        }

        [Test]
        public void An_absent_field_resolves_to_the_vanilla_default()
        {
            var container = new JObject
            {
                [MaterialExtras.Key] = new JObject { ["renderState"] = new JObject() },
            };

            MaterialExtras.TryRead(container, out var extras);
            var state = extras.RenderState;

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.Zero));
            Assert.That(state.BlendEnabled, Is.False);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Cull, Is.Null);
            Assert.That(state.AlphaTestCutoff, Is.Null);
            Assert.That(state.Lit, Is.True);
            Assert.That(state.Transparency, Is.EqualTo(TransparencyMode.Filter));
        }

        [Test]
        public void Fields_left_at_their_default_stay_out_of_the_file()
        {
            var payload = new MaterialExtras { RenderState = new RenderStateExtras() }.ToJson();

            Assert.That(((JObject)payload["renderState"]).Count, Is.Zero);
            Assert.That(payload["uvAnimation"], Is.Null);
            Assert.That(payload["opacityAnimation"], Is.Null);
            Assert.That(payload["flipbook"], Is.Null);
        }

        [Test]
        public void The_payload_hangs_off_its_own_member_of_the_extras_object()
        {
            Assert.That(MaterialExtras.TryRead(Carrying(new MaterialExtras()), out _), Is.True);

            Assert.That(MaterialExtras.TryRead(new MaterialExtras().ToJson(), out _), Is.False,
                "the payload is a member of the extras object, never the extras object itself");
        }

        [Test]
        public void Enums_read_back_by_name_so_the_vocabulary_is_the_file_format()
        {
            var payload = new MaterialExtras
            {
                RenderState = new RenderStateExtras { SrcBlend = BlendFactor.DstColor },
            }.ToJson();

            Assert.That(payload["renderState"]["srcBlend"].Value<string>(), Is.EqualTo("DstColor"));
        }
    }
}
