using System;
using Top.Tables.Custom;

namespace Top.Assets.Conversion
{
    /// <summary>
    /// The facts a consumer of an emitted document must agree on: the names
    /// of the nodes, meshes and materials it contains, and the rate its
    /// animations are sampled at. Every one of these is produced here and
    /// matched somewhere else, so a disagreement fails silently - a missing
    /// component, an unbound material, a collider that never appears, a clip
    /// at the wrong speed.
    /// </summary>
    public static class GltfContract
    {
        /// <summary>
        /// Keyframes are sampled at a fixed rate, so a frame index divided by
        /// this value gives its time in seconds. Consumers set their clip
        /// frame rate to match.
        /// </summary>
        public const float AnimationFramesPerSecond = 30f;

        private const string GeometryPrefix = "geom_";
        private const string DummyPrefix = "dummy_";
        private const string HelperPrefix = "helper_";
        private const string LitShellSuffix = "_lit";
        private const string SkeletonSuffix = "_skeleton";

        /// <summary>
        /// The node and mesh carrying a geometry object, and the animation
        /// clip that drives it.
        /// </summary>
        public static string Geometry(uint objectId)
        {
            return GeometryPrefix + objectId;
        }

        /// <summary>
        /// The child node holding a geometry object's lit subset, which the
        /// game shows only on socketed items.
        /// </summary>
        public static string LitShell(string geometryName)
        {
            return geometryName + LitShellSuffix;
        }

        /// <summary>
        /// Whether a node is a lit shell, and so starts out hidden.
        /// </summary>
        public static bool IsLitShell(string name)
        {
            return name.EndsWith(LitShellSuffix, StringComparison.Ordinal);
        }

        /// <summary>
        /// The subtree holding a skinned geometry object's bones, kept as a
        /// sibling so the bones do not inherit the mesh node's transform.
        /// </summary>
        public static string SkeletonContainer(string geometryName)
        {
            return geometryName + SkeletonSuffix;
        }

        /// <summary>
        /// A locator node, positioned but never rendered.
        /// </summary>
        public static string Dummy(uint dummyId)
        {
            return DummyPrefix + dummyId;
        }

        /// <summary>
        /// A collision mesh. The index disambiguates helpers that share a
        /// name, or have none.
        /// </summary>
        public static string Helper(string meshName, int index)
        {
            return string.IsNullOrEmpty(meshName)
                ? HelperPrefix + "mesh_" + index
                : HelperPrefix + meshName.ToLowerInvariant() + "_" + index;
        }

        /// <summary>
        /// Whether a node is a collision mesh, and so takes a collider
        /// instead of a renderer.
        /// </summary>
        public static bool IsHelper(string name)
        {
            return name.StartsWith(HelperPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// A material, one per mesh subset. Consumers match the asset they
        /// bind by this name.
        /// </summary>
        public static string Material(string modelName, uint objectId, int subset)
        {
            return modelName + "_" + objectId + "_" + subset;
        }

        /// <summary>
        /// The animation clip for a character action, prefixed with the
        /// model name so identically numbered actions of different rigs
        /// stay tellable apart in pickers, which show nothing but the
        /// (often truncated) name. Action numbers below 1 collapse to the
        /// whole-timeline clip.
        /// </summary>
        public static string ActionClip(string modelName, int actionNo)
        {
            if (actionNo < 1)
            {
                return modelName + "_timeline";
            }

            return CharacterActionNames.TryGetName(actionNo, out var slug)
                ? $"{modelName}_{actionNo:00}_{slug}"
                : $"{modelName}_{actionNo:00}";
        }
    }
}
