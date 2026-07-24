using System.Collections.Generic;
using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Rebinds a part's SkinnedMeshRenderer onto another skeleton's
    /// bones by name - the runtime analog of the part files' bone
    /// remap tables, valid because one converter emits both
    /// hierarchies. A bone with no match warns and binds to the
    /// skeleton root so the mesh stays visible instead of crashing.
    /// </summary>
    public static class SkinnedPartBinder
    {
        public static SkinnedMeshRenderer Bind(SkinnedMeshRenderer source, Transform root,
            IReadOnlyDictionary<string, Transform> bones, Object context = null)
        {
            var child = new GameObject(source.name);

            child.transform.SetParent(root, false);

            var target = child.AddComponent<SkinnedMeshRenderer>();

            target.sharedMesh = source.sharedMesh;
            target.sharedMaterials = source.sharedMaterials;
            target.localBounds = source.localBounds;
            target.updateWhenOffscreen = source.updateWhenOffscreen;

            var sourceBones = source.bones;
            var mapped = new Transform[sourceBones.Length];

            for (var i = 0; i < sourceBones.Length; i++)
            {
                if (sourceBones[i] != null && bones.TryGetValue(sourceBones[i].name, out var bone))
                {
                    mapped[i] = bone;
                }
                else
                {
                    var name = sourceBones[i] != null ? sourceBones[i].name : "<null>";

                    Debug.LogWarning($"[Top] part '{source.name}': no bone named " +
                        $"'{name}' in the rig", context);
                    mapped[i] = root;
                }
            }

            target.bones = mapped;
            target.rootBone = source.rootBone != null
                && bones.TryGetValue(source.rootBone.name, out var rootBone)
                    ? rootBone
                    : root;

            return target;
        }
    }
}
