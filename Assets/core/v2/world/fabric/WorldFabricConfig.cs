using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Master configuration for the World Fabric system.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldFabricConfig", menuName = "Starfire/World Fabric/World Fabric Config")]
    public class WorldFabricConfig : ScriptableObject
    {
        [Header("Layer Configs")]
        public SpaceZoneConfig spaceZoneConfig;
        public ColorDistributionConfig colorDistributionConfig;
        public ResourceConfig resourceConfig;
        public FactionConfig factionConfig;
        public CelestialBodyConfig celestialBodyConfig;

        [Header("Debug")]
        public bool showDebugGizmos = false;
        public bool logLayerEvents = false;
    }
}
