using System.IO;
using NUnit.Framework;
using Top.Engine.Editor;
using Top.Tables.Records;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Tests
{
    public class ItemDefinitionTests
    {
        private const int TestId = 9999;
        private const string PartPrefabPath = "Assets/ItemDefinitionTests_part.prefab";

        private GameObject _part;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("part");
            _part = PrefabUtility.SaveAsPrefabAsset(go, PartPrefabPath);
            Object.DestroyImmediate(go);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(PartPrefabPath);
            AssetDatabase.DeleteAsset(ItemDefinition.AssetPath(TestId));
        }

        private static ItemInfoRecord Item(string module)
        {
            return new ItemInfoRecord
            {
                Id = TestId,
                Name = "Test Item",
                Modules = new[] { "0", module, "0", "0", "0" }
            };
        }

        [Test]
        public void WritesDefinitionReferencingResolvedModels()
        {
            ItemConversion.EnsureDefinition(Item("1234"), module => module == "1234" ? _part : null);

            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDefinition.AssetPath(TestId));
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.id, Is.EqualTo(TestId));
            Assert.That(definition.itemName, Is.EqualTo("Test Item"));
            Assert.That(definition.models[0], Is.EqualTo(_part));
            Assert.That(definition.models[1], Is.Null);
        }

        [Test]
        public void SkipsItemWithNothingResolved()
        {
            ItemConversion.EnsureDefinition(Item("1234"), module => null);

            Assert.That(File.Exists(ItemDefinition.AssetPath(TestId)), Is.False);
        }

        [Test]
        public void RegeneratesInPlaceKeepingGuid()
        {
            ItemConversion.EnsureDefinition(Item("1234"), module => _part);
            var guid = AssetDatabase.AssetPathToGUID(ItemDefinition.AssetPath(TestId));

            ItemConversion.EnsureDefinition(
                new ItemInfoRecord
                {
                    Id = TestId,
                    Name = "Renamed",
                    Modules = new[] { "0", "1234", "0", "0", "0" }
                },
                module => _part);

            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDefinition.AssetPath(TestId));
            Assert.That(definition.itemName, Is.EqualTo("Renamed"));
            Assert.That(AssetDatabase.AssetPathToGUID(ItemDefinition.AssetPath(TestId)), Is.EqualTo(guid));
        }
    }
}
