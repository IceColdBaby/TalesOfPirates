using NUnit.Framework;
using Top.Engine.Editor;

namespace Top.Engine.Tests
{
    public class ImporterSettingsTests
    {
        private string _saved;

        [SetUp]
        public void SetUp()
        {
            // The raw field, not the fallback-resolved ClientRoot: restoring
            // the resolved value would pin the fallback into UserSettings.
            _saved = ImporterSettings.instance.RawClientRoot;
        }

        [TearDown]
        public void TearDown()
        {
            ImporterSettings.instance.ClientRoot = _saved;
        }

        [Test]
        public void DerivedPathsFollowClientRoot()
        {
            ImporterSettings.instance.ClientRoot = "C:/client/assets";

            Assert.That(ImporterSettings.instance.ModelRoot.Replace('\\', '/'),
                Is.EqualTo("C:/client/assets/model"));
            Assert.That(ImporterSettings.instance.AnimationRoot.Replace('\\', '/'),
                Is.EqualTo("C:/client/assets/animation"));
            Assert.That(ImporterSettings.instance.TablePath("iteminfo.txt").Replace('\\', '/'),
                Is.EqualTo("C:/client/assets/scripts/table/iteminfo.txt"));
            Assert.That(ImporterSettings.instance.CharacterActionPath.Replace('\\', '/'),
                Is.EqualTo("C:/client/assets/scripts/txt/CharacterAction.tx"));
        }

        [Test]
        public void MissingRootIsInvalid()
        {
            ImporterSettings.instance.ClientRoot = "C:/no/such/folder";

            Assert.That(ImporterSettings.instance.IsValid, Is.False);
        }
    }
}
