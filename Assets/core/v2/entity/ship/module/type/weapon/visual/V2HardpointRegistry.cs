using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Caches and provides lookup for all V2HardpointMarker components on a ship.
    /// Should be attached to the ship root GameObject.
    /// </summary>
    public class V2HardpointRegistry : MonoBehaviour
    {
        private Dictionary<string, V2HardpointMarker> _hardpoints = new();
        private bool _initialized = false;

        /// <summary>
        /// Number of registered hardpoints.
        /// </summary>
        public int Count => _hardpoints.Count;

        /// <summary>
        /// Whether the registry has been initialized.
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// Scans child objects for V2HardpointMarker components and caches them.
        /// Call this before any module equipment.
        /// </summary>
        public void Initialize()
        {
            _hardpoints.Clear();

            var markers = GetComponentsInChildren<V2HardpointMarker>(true);

            foreach (var marker in markers)
            {
                if (string.IsNullOrEmpty(marker.SlotId))
                {
                    Debug.LogWarning($"V2HardpointMarker on '{marker.gameObject.name}' has no SlotId assigned.", marker);
                    continue;
                }

                if (_hardpoints.ContainsKey(marker.SlotId))
                {
                    Debug.LogWarning($"Duplicate V2HardpointMarker SlotId '{marker.SlotId}' found. Only the first will be registered.", marker);
                    continue;
                }

                _hardpoints.Add(marker.SlotId, marker);
            }

            _initialized = true;
        }

        /// <summary>
        /// Gets the hardpoint marker for a given slot ID.
        /// </summary>
        /// <param name="slotId">The slot ID to look up.</param>
        /// <returns>The hardpoint marker, or null if not found.</returns>
        public V2HardpointMarker GetHardpoint(string slotId)
        {
            if (!_initialized)
            {
                Debug.LogWarning("V2HardpointRegistry.GetHardpoint called before Initialize()");
                return null;
            }

            _hardpoints.TryGetValue(slotId, out var marker);
            return marker;
        }

        /// <summary>
        /// Checks if a hardpoint with the given slot ID exists.
        /// </summary>
        /// <param name="slotId">The slot ID to check.</param>
        /// <returns>True if the hardpoint exists, false otherwise.</returns>
        public bool HasHardpoint(string slotId)
        {
            return _hardpoints.ContainsKey(slotId);
        }

        /// <summary>
        /// Gets all registered hardpoints.
        /// </summary>
        /// <returns>An enumerable of all hardpoint markers.</returns>
        public IEnumerable<V2HardpointMarker> GetAllHardpoints()
        {
            return _hardpoints.Values;
        }

        /// <summary>
        /// Gets all registered slot IDs.
        /// </summary>
        /// <returns>An enumerable of all slot IDs.</returns>
        public IEnumerable<string> GetAllSlotIds()
        {
            return _hardpoints.Keys;
        }

        /// <summary>
        /// Gets all hardpoints of a specific weight class.
        /// </summary>
        /// <param name="weightClass">The weight class to filter by.</param>
        /// <returns>An enumerable of matching hardpoint markers.</returns>
        public IEnumerable<V2HardpointMarker> GetHardpointsByWeightClass(WeaponWeightClass weightClass)
        {
            foreach (var marker in _hardpoints.Values)
            {
                if (marker.WeightClass == weightClass)
                {
                    yield return marker;
                }
            }
        }

        /// <summary>
        /// Gets all hardpoints that can mount a weapon of the given weight class.
        /// </summary>
        /// <param name="weaponClass">The weapon weight class.</param>
        /// <returns>An enumerable of compatible hardpoint markers.</returns>
        public IEnumerable<V2HardpointMarker> GetCompatibleHardpoints(WeaponWeightClass weaponClass)
        {
            foreach (var marker in _hardpoints.Values)
            {
                if (marker.CanMount(weaponClass))
                {
                    yield return marker;
                }
            }
        }
    }
}
