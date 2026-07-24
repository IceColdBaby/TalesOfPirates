#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Editor-only source for composed-character content: converted
    /// assets through the asset database. A shipped game replaces this
    /// seam with addressable assets; resolution stays keyed by ids so a
    /// mod layer can intercept the lookup later.
    /// </summary>
    public static class EditorContentSource
    {
        public static GameObject LoadRig(int model)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Content/Character/Rigs/{model:D4}/{model:D4}.glb");
        }

        public static ItemDefinition LoadItemDefinition(int id)
        {
            return AssetDatabase.LoadAssetAtPath<ItemDefinition>(ItemDefinition.AssetPath(id));
        }
    }
}
#endif
