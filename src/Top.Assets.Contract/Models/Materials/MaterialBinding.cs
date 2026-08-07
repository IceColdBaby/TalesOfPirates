using System.Collections.Generic;
using System.Linq;
using Top.Gltf;

namespace Top.Assets.Contract.Models.Materials
{
    /// <summary>
    /// Where a material lands once a document is realized: the node carrying
    /// the renderer and the sub-mesh slot the material occupies on it. A
    /// primitive split onto another mesh, a lit shell for instance, binds to
    /// the node that mesh hangs on.
    /// </summary>
    public readonly struct MaterialBinding
    {
        public MaterialBinding(string nodeName, int subMesh)
        {
            NodeName = nodeName;
            SubMesh = subMesh;
        }

        public string NodeName { get; }

        public int SubMesh { get; }

        public static ILookup<int, MaterialBinding> In(GltfDocument document)
        {
            return Walk(document).ToLookup(entry => entry.Material, entry => entry.Binding);
        }

        private static IEnumerable<Entry> Walk(GltfDocument document)
        {
            if (document.Nodes == null || document.Meshes == null)
            {
                yield break;
            }

            foreach (var node in document.Nodes)
            {
                if (node.Mesh == null || node.Mesh.Value >= document.Meshes.Count)
                {
                    continue;
                }

                var primitives = document.Meshes[node.Mesh.Value].Primitives;

                for (var slot = 0; slot < primitives.Count; slot++)
                {
                    if (primitives[slot].Material != null)
                    {
                        yield return new Entry
                        {
                            Material = primitives[slot].Material.Value,
                            Binding = new MaterialBinding(node.Name, slot),
                        };
                    }
                }
            }
        }

        private class Entry
        {
            public int Material;
            public MaterialBinding Binding;
        }
    }
}
