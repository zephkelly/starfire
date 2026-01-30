using System.Linq;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Calculates ship state heuristics from module data.
    /// Called each frame by UpdateHeuristicsAction.
    /// </summary>
    public static class ShipHeuristics
    {
        public static HeuristicData Calculate(ShipModuleSlotCollection modules, BTContext context)
        {
            var data = new HeuristicData();

            CalculateDefenseHeuristics(modules, ref data);
            CalculateThreatHeuristics(modules, context, ref data);
            CalculateSituationalHeuristics(context, ref data);
            CalculateCapabilities(modules, ref data);

            return data;
        }

        private static void CalculateDefenseHeuristics(ShipModuleSlotCollection modules, ref HeuristicData data)
        {
            // Hull
            var hull = modules.GetAllModulesOfType<IShipHullModule>().FirstOrDefault();
            data.HullPercent = hull != null && hull.MaxIntegrity > 0
                ? hull.CurrentIntegrity / hull.MaxIntegrity
                : 1f;

            // Shield - not yet implemented in v2, default to 0
            data.ShieldPercent = 0f;

            // Overall defense (weighted: hull is more critical)
            data.OverallDefense = (data.HullPercent * 0.7f) + (data.ShieldPercent * 0.3f);

            // Confidence: high when healthy, drops sharply below 50%
            data.Confidence = Mathf.Clamp01(data.OverallDefense * 1.5f);

            // Skittishness: inverse curve, rises sharply when damaged
            data.Skittishness = 1f - Mathf.Pow(data.OverallDefense, 0.5f);

            // Vulnerability: high when shields are down but hull is okay
            data.Vulnerability = (1f - data.ShieldPercent) * data.HullPercent;
        }

        private static void CalculateThreatHeuristics(ShipModuleSlotCollection modules, BTContext context, ref HeuristicData data)
        {
            data.NearbyHostileCount = 0;
            data.ClosestThreatDistance = float.MaxValue;
            data.ThreatLevel = 0f;
            data.UnidentifiedContactCount = 0;
            data.HasUnidentifiedContacts = false;
            data.ClosestUnidentifiedDistance = float.MaxValue;

            var sensor = modules.GetAllModulesOfType<ISensorModule>().FirstOrDefault();
            if (sensor == null) return;

            // Process hostile entities
            var hostiles = sensor.GetHostileEntities().ToList();
            if (hostiles.Count > 0)
            {
                data.NearbyHostileCount = hostiles.Count;

                foreach (var hostile in hostiles)
                {
                    if (hostile.Distance < data.ClosestThreatDistance)
                    {
                        data.ClosestThreatDistance = hostile.Distance;
                    }
                }

                float countFactor = Mathf.Clamp01(hostiles.Count / 5f);
                float distanceFactor = data.ClosestThreatDistance < 50f
                    ? 1f - (data.ClosestThreatDistance / 50f)
                    : 0f;
                data.ThreatLevel = Mathf.Max(countFactor, distanceFactor);
            }

            // Process unidentified contacts (detection level below Full)
            foreach (var entity in sensor.DetectedEntities)
            {
                if (entity.Level < V2DetectionLevel.Full)
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
            data.HasTarget = context.Has("steering_target") ||
                             context.Has("investigation_target") ||
                             context.Has("combat_target");

            data.IsInCombat = context.Has("combat_target") ||
                              (data.NearbyHostileCount > 0 && data.ClosestThreatDistance < 30f);
        }

        private static void CalculateCapabilities(ShipModuleSlotCollection modules, ref HeuristicData data)
        {
            // Propulsion
            var propulsion = modules.GetAllModulesOfType<IShipPropulsionModule>().FirstOrDefault();
            data.HasPropulsionModule = propulsion != null;
            data.MaxSpeed = propulsion?.MaxSpeed ?? 0f;
            data.MaxAcceleration = propulsion?.Acceleration ?? 0f;

            // Sensor
            var sensor = modules.GetAllModulesOfType<ISensorModule>().FirstOrDefault();
            data.HasSensorModule = sensor != null;
            data.SensorRange = sensor?.DetectionRange ?? 0f;
            data.SilhouetteRange = sensor?.SilhouetteRange ?? 0f;

            // Module flags
            data.HasTransponderModule = modules.GetAllModulesOfType<ITransponderModule>().Any();
            data.HasWeaponModules = modules.CountModulesInCategory(ShipModuleCategory.Offense) > 0;
            data.HasShieldModule = false; // Shield not yet in v2

            // Faction/Identity
            var transponder = modules.GetAllModulesOfType<ITransponderModule>().FirstOrDefault();
            data.OwnFaction = transponder?.Faction ?? default;
        }
    }
}
