using NUnit.Framework;
using Top.Assets.Conversion.Pipeline;

namespace Top.Assets.Conversion.Tests.Pipeline
{
    public class ConversionSettingsTests
    {
        private static readonly ConversionSettings Settings =
            new ConversionSettings("C:/client/assets", "C:/project/Assets/Content");

        private static string Slashed(string path) => path.Replace('\\', '/');

        [Test]
        public void Client_roots_follow_the_client_root()
        {
            Assert.That(Slashed(Settings.Source.Models), Is.EqualTo("C:/client/assets/model"));
            Assert.That(Slashed(Settings.Source.Textures), Is.EqualTo("C:/client/assets/texture"));
            Assert.That(Slashed(Settings.Source.Animations), Is.EqualTo("C:/client/assets/animation"));
        }

        [Test]
        public void Client_files_sit_under_their_root()
        {
            Assert.That(Slashed(Settings.Source.Model("scene", "stone01.lgo")),
                Is.EqualTo("C:/client/assets/model/scene/stone01.lgo"));
            Assert.That(Slashed(Settings.Source.Skeleton(724)),
                Is.EqualTo("C:/client/assets/animation/0724.lab"));
            Assert.That(Slashed(Settings.Source.Skeleton("0724")),
                Is.EqualTo("C:/client/assets/animation/0724.lab"));
            Assert.That(Slashed(Settings.Source.Table("iteminfo.txt")),
                Is.EqualTo("C:/client/assets/scripts/table/iteminfo.txt"));
            Assert.That(Slashed(Settings.Source.CharacterAction),
                Is.EqualTo("C:/client/assets/scripts/txt/CharacterAction.tx"));
        }

        [Test]
        public void Client_texture_folders_are_lowercase()
        {
            Assert.That(Slashed(Settings.Source.TextureDir("Scene")),
                Is.EqualTo("C:/client/assets/texture/scene"));
        }

        [Test]
        public void Output_paths_follow_the_output_root()
        {
            Assert.That(Slashed(Settings.Output.ModelDir("Scene", "stone01")),
                Is.EqualTo("C:/project/Assets/Content/Scene/Models/stone01"));
            Assert.That(Slashed(Settings.Output.RigDir("0001")),
                Is.EqualTo("C:/project/Assets/Content/Character/Rigs/0001"));
            Assert.That(Slashed(Settings.Output.TextureDir("Item")),
                Is.EqualTo("C:/project/Assets/Content/Textures/Item"));
        }

        [Test]
        public void Converted_models_are_named_after_what_they_came_from()
        {
            Assert.That(Slashed(Settings.Output.Model("Scene", "stone01")),
                Is.EqualTo("C:/project/Assets/Content/Scene/Models/stone01/stone01.glb"));
            Assert.That(Slashed(Settings.Output.Rig("0001")),
                Is.EqualTo("C:/project/Assets/Content/Character/Rigs/0001/0001.glb"));
        }
    }
}
