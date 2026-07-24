using System.Collections.Generic;
using UnityEngine;

namespace Top.Engine
{
    /// <summary>
    /// Composes a player-style character: instantiates
    /// the model's rig prefab (whose imported Animation auto-plays
    /// the waiting clip) and binds one skinned part per equipment
    /// slot onto its bones, mirroring the client's five-slot LoadCha
    /// path, plus held weapons on the hand dummies. Composes in edit
    /// mode and play mode alike; editing the fields re-composes.
    /// Content access is editor-only for now; a player build logs an
    /// error and shows nothing.
    /// </summary>
    [ExecuteAlways]
    public sealed class PlayerCharacter : MonoBehaviour
    {
        public int model;
        public int hairItem;
        public int faceItem;
        public int bodyItem;
        public int gloveItem;
        public int shoesItem;
        public int rightWeaponItem;
        public int leftWeaponItem;

        private GameObject _composed;
        private bool _dirty;

        private void OnEnable()
        {
            Compose();
            _dirty = false;
        }

        private void OnDisable()
        {
            Teardown();
        }

        private void OnValidate()
        {
            _dirty = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += ComposeDeferred;
            }
#endif
        }

        private void Update()
        {
            if (_dirty && Application.isPlaying)
            {
                _dirty = false;
                Compose();
            }
        }

        private void Teardown()
        {
            if (_composed == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_composed);
            }
            else
            {
                DestroyImmediate(_composed);
            }

            _composed = null;
        }

#if UNITY_EDITOR
        private void ComposeDeferred()
        {
            UnityEditor.EditorApplication.delayCall -= ComposeDeferred;

            if (this == null || Application.isPlaying || !isActiveAndEnabled || !_dirty)
            {
                return;
            }

            _dirty = false;
            Compose();
        }
#endif

        public void Compose()
        {
#if UNITY_EDITOR
            Teardown();

            var rigPrefab = EditorContentSource.LoadRig(model);

            if (rigPrefab == null)
            {
                Debug.LogError($"[Top] no rig for model {model}", this);
                return;
            }

            _composed = Instantiate(rigPrefab, transform);
            // The composed hierarchy is derived state: it must never be
            // saved into the scene or a build, only the fields are.
            _composed.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            var bones = new Dictionary<string, Transform>();

            foreach (var node in _composed.GetComponentsInChildren<Transform>(true))
            {
                if (!bones.TryAdd(node.name, node))
                {
                    Debug.LogWarning($"[Top] rig has more than one node named " +
                        $"'{node.name}'; parts bind to the first", this);
                }
            }

            ComposeSlot(hairItem, 0, bones);
            ComposeSlot(faceItem, 1, bones);
            ComposeSlot(bodyItem, 2, bones);
            ComposeSlot(gloveItem, 3, bones);
            ComposeSlot(shoesItem, 4, bones);
            ComposeWeapon(rightWeaponItem, WeaponHand.Right, bones);
            ComposeWeapon(leftWeaponItem, WeaponHand.Left, bones);

            foreach (var node in _composed.GetComponentsInChildren<Transform>(true))
            {
                node.gameObject.hideFlags = _composed.hideFlags;
            }
#else
            Debug.LogError("[Top] PlayerCharacter composes in the editor only", this);
#endif
        }

#if UNITY_EDITOR
        private void ComposeSlot(int itemId, int slot, Dictionary<string, Transform> bones)
        {
            if (itemId == 0)
            {
                return;
            }

            var definition = EditorContentSource.LoadItemDefinition(itemId);

            if (definition == null)
            {
                Debug.LogWarning($"[Top] slot {slot}: item {itemId} is not converted yet; " +
                    "convert it via Top/Importer", this);
                return;
            }

            var partModel = model >= 0 && model < definition.models.Length ? definition.models[model] : null;

            if (partModel == null)
            {
                Debug.LogWarning($"[Top] item {definition.id} '{definition.itemName}' has no model " +
                    $"for framework {model}", this);
                return;
            }

            var source = partModel.GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (source == null)
            {
                Debug.LogWarning($"[Top] item {definition.id} model has no skinned mesh", this);
                return;
            }

            SkinnedPartBinder.Bind(source, _composed.transform, bones, this);
        }

        private void ComposeWeapon(int itemId, WeaponHand hand, Dictionary<string, Transform> bones)
        {
            if (itemId == 0)
            {
                return;
            }

            var definition = EditorContentSource.LoadItemDefinition(itemId);

            if (definition == null)
            {
                Debug.LogWarning($"[Top] weapon item {itemId} is not converted yet; " +
                    "convert it via Top/Importer", this);
                return;
            }

            var weaponPrefab = model >= 0 && model < definition.models.Length ? definition.models[model] : null;

            if (weaponPrefab == null)
            {
                Debug.LogWarning($"[Top] weapon {definition.id} '{definition.itemName}' has no model " +
                    $"for framework {model}", this);
                return;
            }

            WeaponAttachment.Attach(weaponPrefab, hand, bones, this);
        }
#endif
    }
}
