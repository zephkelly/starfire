using System;

namespace StarfireV2
{
    /// <summary>
    /// Contains all computed heuristic values for a ship.
    /// Updated each frame by UpdateHeuristicsAction and written to blackboard for BT access.
    /// This is the canonical perception layer - all BT decisions should read from this data.
    /// </summary>
    [Serializable]
    public struct HeuristicData
    {
        // Health/Defense (0-1 normalized)
        public float HullPercent;
        public float ShieldPercent;
        public float OverallDefense;

        // Derived States (0-1)
        public float Confidence;
        public float Skittishness;
        public float Vulnerability;

        // Threat Assessment
        public int NearbyHostileCount;
        public float ClosestThreatDistance;
        public float ThreatLevel;

        // Detection/Awareness
        public int UnidentifiedContactCount;
        public bool HasUnidentifiedContacts;
        public float ClosestUnidentifiedDistance;

        // Situational
        public bool HasTarget;
        public bool IsInCombat;

        // Propulsion Capabilities
        public float MaxSpeed;
        public float MaxAcceleration;
        public bool HasPropulsionModule;

        // Sensor Capabilities
        public float SensorRange;
        public float SilhouetteRange;
        public bool HasSensorModule;

        // Module Capabilities
        public bool HasTransponderModule;
        public bool HasWeaponModules;
        public bool HasShieldModule;

        // Faction/Identity
        public V2FactionData OwnFaction;
    }
}
