using UnityEngine;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Static utility to extract simulation-relevant capabilities from a ShipEntity.
    /// Creates a lightweight SimulatedShipCapabilities struct without Unity references.
    /// </summary>
    public static class ShipCapabilitiesExtractor
    {
        /// <summary>
        /// Extract capabilities from a ShipEntity's modules.
        /// </summary>
        /// <param name="ship">The ship entity to extract from.</param>
        /// <returns>A populated SimulatedShipCapabilities struct.</returns>
        public static SimulatedShipCapabilities Extract(ShipEntity ship)
        {
            if (ship == null)
            {
                Debug.LogWarning("[ShipCapabilitiesExtractor] Null ship provided, returning defaults.");
                return SimulatedShipCapabilities.Default;
            }

            var caps = SimulatedShipCapabilities.Default;

            // ── Extract Shield Module ────────────────────────────────────────
            var shield = ship.Shield;
            if (shield != null)
            {
                caps.MaxShield = shield.MaxShield;
                caps.CurrentShield = shield.CurrentShield;
                caps.ShieldRegenRate = shield.RegenRate;
                caps.ShieldRechargeDelay = shield.RechargeDelay;
            }

            // ── Extract Hull Module ──────────────────────────────────────────
            var hull = ship.Hull;
            if (hull != null)
            {
                caps.MaxIntegrity = hull.MaxIntegrity;
                caps.CurrentIntegrity = hull.CurrentIntegrity;
            }

            // ── Aggregate Offensive Modules ──────────────────────────────────
            float totalDps = 0f;
            float maxRange = 0f;
            int weaponCount = 0;

            foreach (var weapon in ship.OffenseModules)
            {
                if (weapon == null) continue;

                // DPS = Damage * FireRate (shots per second)
                totalDps += weapon.Damage * weapon.FireRate;

                // Track longest range
                if (weapon.Range > maxRange)
                    maxRange = weapon.Range;

                weaponCount++;
            }

            caps.TotalDamagePerSecond = totalDps;
            caps.MaxWeaponRange = maxRange;
            caps.WeaponCount = weaponCount;

            return caps;
        }

        /// <summary>
        /// Extract capabilities from a ShipController's entity.
        /// Convenience method for use with MonoBehaviour references.
        /// </summary>
        /// <param name="controller">The ship controller.</param>
        /// <returns>A populated SimulatedShipCapabilities struct.</returns>
        public static SimulatedShipCapabilities Extract(IEntityController controller)
        {
            if (controller == null)
            {
                Debug.LogWarning("[ShipCapabilitiesExtractor] Null controller provided, returning defaults.");
                return SimulatedShipCapabilities.Default;
            }

            // Try to get the ShipEntity from the controller
            if (controller is ShipController shipController)
            {
                return Extract(shipController.Ship);
            }

            Debug.LogWarning($"[ShipCapabilitiesExtractor] Controller is not a ShipController, returning defaults.");
            return SimulatedShipCapabilities.Default;
        }
    }
}
