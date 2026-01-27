using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for space zone generation using metaball fields.
    /// Each space property (nebula, asteroids, void, anomaly) has its own
    /// independent field, allowing overlapping states (e.g., asteroids inside nebula).
    /// </summary>
    [CreateAssetMenu(fileName = "SpaceZoneConfig", menuName = "Starfire/World Fabric/Space Zone Config")]
    public class SpaceZoneConfig : ScriptableObject
    {
        [Header("Enable")]
        public bool enabled = true;

        [Header("Nebula Field (Large smooth gas clouds)")]
        [Tooltip("Minimum distance between nebula blob centers")]
        public float nebulaBlobSpacing = 200000f;
        [Tooltip("Minimum nebula blob radius")]
        public float nebulaBlobRadiusMin = 80000f;
        [Tooltip("Maximum nebula blob radius")]
        public float nebulaBlobRadiusMax = 300000f;
        [Tooltip("Blob strength (higher = denser nebula cores)")]
        [Range(0.1f, 2f)]
        public float nebulaBlobStrength = 1f;
        [Tooltip("Falloff power (higher = sharper nebula edges)")]
        [Range(1f, 5f)]
        public float nebulaFalloffPower = 2f;
        [Tooltip("Density threshold for nebula to be 'active'")]
        [Range(0f, 1f)]
        public float nebulaThreshold = 0.3f;

        [Header("Asteroid Field (Scattered belts, can overlap nebula)")]
        [Tooltip("Minimum distance between asteroid blob centers")]
        public float asteroidBlobSpacing = 80000f;
        [Tooltip("Minimum asteroid blob radius")]
        public float asteroidBlobRadiusMin = 30000f;
        [Tooltip("Maximum asteroid blob radius")]
        public float asteroidBlobRadiusMax = 100000f;
        [Tooltip("Blob strength")]
        [Range(0.1f, 2f)]
        public float asteroidBlobStrength = 0.8f;
        [Tooltip("Falloff power")]
        [Range(1f, 5f)]
        public float asteroidFalloffPower = 2.5f;
        [Tooltip("Density threshold for asteroids to be 'active'")]
        [Range(0f, 1f)]
        public float asteroidThreshold = 0.3f;

        [Header("Void Field (Vast empty expanses — suppresses other properties)")]
        [Tooltip("Minimum distance between void blob centers")]
        public float voidBlobSpacing = 300000f;
        [Tooltip("Minimum void blob radius")]
        public float voidBlobRadiusMin = 150000f;
        [Tooltip("Maximum void blob radius")]
        public float voidBlobRadiusMax = 400000f;
        [Tooltip("Blob strength")]
        [Range(0.1f, 2f)]
        public float voidBlobStrength = 1f;
        [Tooltip("Falloff power")]
        [Range(1f, 5f)]
        public float voidFalloffPower = 1.5f;
        [Tooltip("Density threshold for void to be 'active'")]
        [Range(0f, 1f)]
        public float voidThreshold = 0.3f;

        [Header("Anomaly Field (Rare, small strange-physics zones)")]
        [Tooltip("Minimum distance between anomaly blob centers")]
        public float anomalyBlobSpacing = 500000f;
        [Tooltip("Minimum anomaly blob radius")]
        public float anomalyBlobRadiusMin = 20000f;
        [Tooltip("Maximum anomaly blob radius")]
        public float anomalyBlobRadiusMax = 60000f;
        [Tooltip("Blob strength")]
        [Range(0.1f, 2f)]
        public float anomalyBlobStrength = 0.9f;
        [Tooltip("Falloff power")]
        [Range(1f, 5f)]
        public float anomalyFalloffPower = 3f;
        [Tooltip("Density threshold for anomaly to be 'active'")]
        [Range(0f, 1f)]
        public float anomalyThreshold = 0.4f;

        /// <summary>
        /// Create a MetaballFieldConfig for the nebula field.
        /// </summary>
        public MetaballFieldConfig GetNebulaFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = nebulaBlobSpacing,
            blobRadiusMin = nebulaBlobRadiusMin,
            blobRadiusMax = nebulaBlobRadiusMax,
            blobStrength = nebulaBlobStrength,
            falloffPower = nebulaFalloffPower,
            threshold = nebulaThreshold,
        };

        /// <summary>
        /// Create a MetaballFieldConfig for the asteroid field.
        /// </summary>
        public MetaballFieldConfig GetAsteroidFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = asteroidBlobSpacing,
            blobRadiusMin = asteroidBlobRadiusMin,
            blobRadiusMax = asteroidBlobRadiusMax,
            blobStrength = asteroidBlobStrength,
            falloffPower = asteroidFalloffPower,
            threshold = asteroidThreshold,
        };

        /// <summary>
        /// Create a MetaballFieldConfig for the void field.
        /// </summary>
        public MetaballFieldConfig GetVoidFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = voidBlobSpacing,
            blobRadiusMin = voidBlobRadiusMin,
            blobRadiusMax = voidBlobRadiusMax,
            blobStrength = voidBlobStrength,
            falloffPower = voidFalloffPower,
            threshold = voidThreshold,
        };

        /// <summary>
        /// Create a MetaballFieldConfig for the anomaly field.
        /// </summary>
        public MetaballFieldConfig GetAnomalyFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = anomalyBlobSpacing,
            blobRadiusMin = anomalyBlobRadiusMin,
            blobRadiusMax = anomalyBlobRadiusMax,
            blobStrength = anomalyBlobStrength,
            falloffPower = anomalyFalloffPower,
            threshold = anomalyThreshold,
        };
    }
}
