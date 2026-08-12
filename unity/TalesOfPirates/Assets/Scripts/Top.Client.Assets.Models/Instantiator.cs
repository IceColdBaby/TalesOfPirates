using GLTFast;
using GLTFast.Logging;
using Top.Contracts.Assets.Models;
using UnityEngine;

namespace Top.Client.Assets.Models
{
    /// <summary>
    /// Keeps helper_* collision meshes out of rendering, giving them a mesh
    /// filter and no renderer, and exposes the node lookup so clips can be
    /// hosted per subtree after instantiation.
    /// </summary>
    public class Instantiator : GameObjectInstantiator
    {
        public Instantiator(IGltfReadable gltf, Transform parent, ICodeLogger logger = null,
            InstantiationSettings settings = null)
            : base(gltf, parent, logger, settings)
        {
        }

        public Transform Scene => SceneTransform;

        public GameObject NodeObject(uint nodeIndex)
        {
            return m_Nodes != null && m_Nodes.TryGetValue(nodeIndex, out var node) ? node : null;
        }

        public override void AddPrimitive(uint nodeIndex, string meshName, MeshResult meshResult,
            uint[] joints = null, uint? rootJoint = null, float[] morphTargetWeights = null,
            int meshNumeration = 0)
        {
            var node = m_Nodes[nodeIndex];

            if (Naming.IsHelper(node.name))
            {
                node.AddComponent<MeshFilter>().mesh = meshResult.mesh;

                return;
            }

            base.AddPrimitive(nodeIndex, meshName, meshResult, joints, rootJoint,
                morphTargetWeights, meshNumeration);
        }
    }
}
