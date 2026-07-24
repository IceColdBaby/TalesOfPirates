using System.Collections.Generic;
using Top.Assets.Conversion;
using UnityEngine;

namespace Top.Engine
{
    public enum WeaponHand
    {
        Right,
        Left,
    }

    /// <summary>
    /// Glues a rigid item model to a hand: the item hangs under the
    /// skeleton's hand dummy, offset by the inverse of its own grip
    /// dummy so the grip lands exactly on the hand - the original
    /// two-dummy link expressed as parenting.
    /// </summary>
    public static class WeaponAttachment
    {
        // Equip slots double as skeleton dummy ids in the original
        // client: 6 is the left hand, 9 the right.
        private const uint LeftHandDummy = 6;
        private const uint RightHandDummy = 9;
        private const uint GripDummy = 0;

        public static GameObject Attach(GameObject weaponPrefab, WeaponHand hand,
            IReadOnlyDictionary<string, Transform> bones, Object context = null)
        {
            var dummyName = GltfContract.Dummy(
                hand == WeaponHand.Left ? LeftHandDummy : RightHandDummy);

            if (!bones.TryGetValue(dummyName, out var handDummy))
            {
                Debug.LogWarning($"[Top] rig has no '{dummyName}' dummy; weapon skipped",
                    context);
                return null;
            }

            var weapon = Object.Instantiate(weaponPrefab, handDummy, false);
            var gripName = GltfContract.Dummy(GripDummy);
            var grip = FindDeep(weapon.transform, gripName);

            if (grip == null)
            {
                Debug.LogWarning($"[Top] weapon '{weaponPrefab.name}' has no '{gripName}' " +
                    "dummy; attached at the hand origin", context);
                return weapon;
            }

            var relative = weapon.transform.worldToLocalMatrix * grip.localToWorldMatrix;
            // The original inverts the grip dummy without its scale
            // factor; mirror that so a scaled dummy cannot skew the
            // offset.
            var rotation = Quaternion.Inverse(relative.rotation);

            weapon.transform.localRotation = rotation;
            weapon.transform.localPosition = rotation * -(Vector3)relative.GetColumn(3);

            return weapon;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform != root && transform.name == name)
                {
                    return transform;
                }
            }

            return null;
        }
    }
}
