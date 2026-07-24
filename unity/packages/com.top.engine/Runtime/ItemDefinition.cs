using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Converted item data: one entry per player framework (0-3)
    /// referencing the converted asset that framework uses - a skinned
    /// wearable part model for equipment, an item prefab for held
    /// weapons; null when the item has no model for that framework or
    /// it is not converted. Derived from iteminfo during conversion and
    /// regenerated on every conversion touch - never hand-edited.
    /// </summary>
    public sealed class ItemDefinition : ScriptableObject
    {
        public int id;
        public string itemName;
        public GameObject[] models = new GameObject[4];

        /// <summary>
        /// Where the definition for an item id lives; the convention
        /// both the converter and the editor content source use.
        /// </summary>
        public static string AssetPath(int id) => $"Assets/Content/Item/Definitions/{id:D4}.asset";
    }
}
