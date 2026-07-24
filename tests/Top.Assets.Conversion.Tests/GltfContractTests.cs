using NUnit.Framework;
using Top.Assets.Conversion;

namespace Top.Assets.Conversion.Tests
{
    /// <summary>
    /// Pins the agreement between each formatter and its matcher. The formats
    /// themselves are pinned against real models by the mapper tests, which
    /// spell them out as literals so a typo here cannot pass both sides.
    /// </summary>
    public class GltfContractTests
    {
        [Test]
        public void Helpers_match_the_names_they_are_given()
        {
            Assert.That(GltfContract.IsHelper(GltfContract.Helper("Block", 0)), Is.True);
            Assert.That(GltfContract.IsHelper(GltfContract.Helper(string.Empty, 3)), Is.True);
            Assert.That(GltfContract.IsHelper(GltfContract.Geometry(0)), Is.False);
        }

        [Test]
        public void Lit_shells_match_the_names_they_are_given()
        {
            Assert.That(GltfContract.IsLitShell(GltfContract.LitShell(GltfContract.Geometry(7))), Is.True);
        }

        [Test]
        public void Skeleton_containers_are_not_lit_shells()
        {
            // Both hang off the same geom_<id> stem, so a suffix match is the
            // nearest thing in the layout to a false positive.
            Assert.That(
                GltfContract.IsLitShell(GltfContract.SkeletonContainer(GltfContract.Geometry(1))),
                Is.False);
        }
    }
}
