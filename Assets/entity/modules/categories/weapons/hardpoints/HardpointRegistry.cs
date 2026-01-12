using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Caches all HardpointMarker components on a ship for efficient lookup.
    /// Add to ship root GameObject alongside ShipController.
    /// </summary>
    public class HardpointRegistry : MonoBehaviour
    {
        private Dictionary<string, HardpointMarker> _hardpoints = new();
        private bool _initialized = false;

        public bool IsInitialized => _initialized;

        /// <summary>
        /// Scans children for HardpointMarker components and caches them.
        /// Call this after the ship GameObject is fully set up.
        /// </summary>
        public void Initialize()
        {
            _hardpoints.Clear();
            var markers = GetComponentsInChildren<HardpointMarker>();

            foreach (var marker in markers)
            {
                if (string.IsNullOrEmpty(marker.SlotId))
                {
                    Debug.LogWarning($"HardpointMarker on {marker.gameObject.name} has no slotId set", marker);
                    continue;
                }

                if (_hardpoints.ContainsKey(marker.SlotId))
                {
                    Debug.LogWarning($"Duplicate hardpoint slotId '{marker.SlotId}' found on {marker.gameObject.name}", marker);
                    continue;
                }

                _hardpoints[marker.SlotId] = marker;
            }

            _initialized = true;
        }

        /// <summary>
        /// Gets a hardpoint by its slot ID.
        /// </summary>
        /// <param name="slotId">The slot ID to look up</param>
        /// <returns>The HardpointMarker if found, null otherwise</returns>
        public HardpointMarker GetHardpoint(string slotId)
        {
            if (!_initialized)
            {
                Debug.LogWarning("HardpointRegistry.GetHardpoint called before Initialize()", this);
                return null;
            }

            _hardpoints.TryGetValue(slotId, out var marker);
            return marker;
        }

        /// <summary>
        /// Gets all registered hardpoints.
        /// </summary>
        public IEnumerable<HardpointMarker> GetAllHardpoints() => _hardpoints.Values;

        /// <summary>
        /// Gets all registered slot IDs.
        /// </summary>
        public IEnumerable<string> GetAllSlotIds() => _hardpoints.Keys;

        /// <summary>
        /// Returns the number of registered hardpoints.
        /// </summary>
        public int Count => _hardpoints.Count;
    }
}
