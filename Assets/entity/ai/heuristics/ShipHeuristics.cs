using System.Linq;
using Starfire.Entity.AI.BT;
using Starfire.Entity.Modules;
using Starfire.Entity.Modules.Sensor;
using Starfire.Entity.Modules.Transponder;
using UnityEngine;

namespace Starfire.Entity.AI.Heuristics
{
    using Starfire.Entity; // For ShipSystems

    /// <summary>
    /// Calculates ship state heuristics from ShipSystems data.
    /// Called each frame by UpdateHeuristicsAction.
    /// </summary>
    public static class ShipHeuristics
    {
        public static HeuristicData Calculate(ShipSystems systems, BTContext context)
        {
            var data = new HeuristicData();

            CalculateDefenseHeuristics(systems, ref data);
            CalculateThreatHeuristics(systems, context, ref data);
            CalculateSituationalHeuristics(context, ref data);
            CalculateCapabilities(systems, ref data);

            return data;
        }

        private static void CalculateDefenseHeuristics(ShipSystems systems, ref HeuristicData data)
        {
            // Hull
            var hull = systems.PrimaryHull;
            data.HullPercent = hull != null && hull.MaxHealth > 0
                ? (float)hull.CurrentHealth / hull.MaxHealth
                : 1f;

            // Shield
            var shield = systems.PrimaryShield;
            data.ShieldPercent = shield != null && shield.MaxShield > 0
                ? (float)shield.CurrentShield / shield.MaxShield
                : 0f;

            // Overall defense (weighted: hull is more critical)
            data.OverallDefense = (data.HullPercent * 0.7f) + (data.ShieldPercent * 0.3f);

            // Confidence: high when healthy, drops sharply below 50%
            data.Confidence = Mathf.Clamp01(data.OverallDefense * 1.5f);

            // Skittishness: inverse curve, rises sharply when damaged
            data.Skittishness = 1f - Mathf.Pow(data.OverallDefense, 0.5f);

            // Vulnerability: high when shields are down but hull is okay
            // Represents "I could be hurt easily right now"
            data.Vulnerability = (1f - data.ShieldPercent) * data.HullPercent;
        }

        private static void CalculateThreatHeuristics(ShipSystems systems, BTContext context, ref HeuristicData data)
        {
            data.NearbyHostileCount = 0;
            data.ClosestThreatDistance = float.MaxValue;
            data.ThreatLevel = 0f;
            data.UnidentifiedContactCount = 0;
            data.HasUnidentifiedContacts = false;
            data.ClosestUnidentifiedDistance = float.MaxValue;

            // Get sensor module
            var sensor = systems.GetAllModulesOfType<ISensorShipModule>().FirstOrDefault();
            if (sensor == null) return;

            // Process hostile entities
            var hostiles = sensor.GetHostileEntities().ToList();
            if (hostiles.Count > 0)
            {
                data.NearbyHostileCount = hostiles.Count;

                // Find closest threat using the Distance property already calculated by sensor
                foreach (var hostile in hostiles)
                {
                    if (hostile.Distance < data.ClosestThreatDistance)
                    {
                        data.ClosestThreatDistance = hostile.Distance;
                    }
                }

                // Calculate threat level based on count and proximity
                float countFactor = Mathf.Clamp01(hostiles.Count / 5f);
                float distanceFactor = data.ClosestThreatDistance < 50f
                    ? 1f - (data.ClosestThreatDistance / 50f)
                    : 0f;
                data.ThreatLevel = Mathf.Max(countFactor, distanceFactor);
            }

            // Process unidentified contacts (detection level below Full)
            foreach (var entity in sensor.DetectedEntities)
            {
                if (entity.Level < DetectionLevel.Full)
                {
                    data.UnidentifiedContactCount++;

                    if (entity.Distance < data.ClosestUnidentifiedDistance)
                    {
                        data.ClosestUnidentifiedDistance = entity.Distance;
                    }
                }
            }

            data.HasUnidentifiedContacts = data.UnidentifiedContactCount > 0;
        }

        private static void CalculateSituationalHeuristics(BTContext context, ref HeuristicData data)
        {
            // Check if we have an active target
            data.HasTarget = context.Has("steering_target") ||
                             context.Has("investigation_target") ||
                             context.Has("combat_target");

            // Check if we're in combat (has hostile target or recently took damage)
            data.IsInCombat = context.Has("combat_target") ||
                              (data.NearbyHostileCount > 0 && data.ClosestThreatDistance < 30f);
        }

        private static void CalculateCapabilities(ShipSystems systems, ref HeuristicData data)
        {
            // Propulsion
            var propulsion = systems.PrimaryImpulse;
            data.HasPropulsionModule = propulsion != null;
            data.MaxSpeed = propulsion?.MaxSpeed ?? 0f;
            data.MaxAcceleration = propulsion?.Acceleration ?? 0f;

            // Sensor
            var sensor = systems.GetAllModulesOfType<ISensorShipModule>().FirstOrDefault();
            data.HasSensorModule = sensor != null;
            data.SensorRange = sensor?.DetectionRange ?? 0f;
            data.SilhouetteRange = sensor?.SilhouetteRange ?? 0f;

            // Module flags
            data.HasTransponderModule = systems.GetAllModulesOfType<ITransponderShipModule>().Any();
            data.HasWeaponModules = systems.CountModulesInCategory(ModuleCategory.Weapons) > 0;
            data.HasShieldModule = systems.PrimaryShield != null;

            // Faction/Identity
            var transponder = systems.GetAllModulesOfType<ITransponderShipModule>().FirstOrDefault();
            data.OwnFaction = transponder?.Faction ?? default;
        }
    }
}
