namespace Starfire.Entity.AI.Heuristics
{
    /// <summary>
    /// Blackboard key constants for heuristic values.
    /// </summary>
    public static class HeuristicKeys
    {
        // Full struct for goal evaluation
        public const string HeuristicData = "heuristics";

        // Health/Defense (0-1 normalized)
        public const string HullPercent = "heuristic_hull_percent";
        public const string ShieldPercent = "heuristic_shield_percent";
        public const string OverallDefense = "heuristic_overall_defense";

        // Derived States (0-1)
        public const string Confidence = "heuristic_confidence";
        public const string Skittishness = "heuristic_skittishness";
        public const string Vulnerability = "heuristic_vulnerability";

        // Threat Assessment
        public const string ThreatLevel = "heuristic_threat_level";
        public const string NearbyHostileCount = "heuristic_hostile_count";
        public const string ClosestThreatDistance = "heuristic_closest_threat_distance";

        // Detection/Awareness
        public const string UnidentifiedContactCount = "heuristic_unidentified_count";
        public const string HasUnidentifiedContacts = "heuristic_has_unidentified";
        public const string ClosestUnidentifiedDistance = "heuristic_closest_unidentified_distance";

        // Situational
        public const string HasTarget = "heuristic_has_target";
        public const string IsInCombat = "heuristic_is_in_combat";

        // Propulsion Capabilities
        public const string MaxSpeed = "heuristic_max_speed";
        public const string MaxAcceleration = "heuristic_max_acceleration";
        public const string HasPropulsionModule = "heuristic_has_propulsion";

        // Sensor Capabilities
        public const string SensorRange = "heuristic_sensor_range";
        public const string SilhouetteRange = "heuristic_silhouette_range";
        public const string HasSensorModule = "heuristic_has_sensor";

        // Module Flags
        public const string HasTransponderModule = "heuristic_has_transponder";
        public const string HasWeaponModules = "heuristic_has_weapons";
        public const string HasShieldModule = "heuristic_has_shield";
    }
}
