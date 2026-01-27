using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for resource distribution generation using metaball fields.
    /// Each resource category (Mineral, Ore, Gas, Exotic, Water) has its own
    /// independent field, allowing overlapping resource deposits.
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceConfig", menuName = "Starfire/World Fabric/Resource Config")]
    public class ResourceConfig : ScriptableObject
    {
        [Header("Enable")]
        public bool enabled = true;

        [Header("Mineral Field (Asteroid-correlated solid deposits)")]
        [Tooltip("Minimum distance between mineral blob centers")]
        public float mineralBlobSpacing = 120000f;
        [Tooltip("Minimum mineral blob radius")]
        public float mineralBlobRadiusMin = 40000f;
        [Tooltip("Maximum mineral blob radius")]
        public float mineralBlobRadiusMax = 150000f;
        [Tooltip("Blob strength")]
        [Range(0.1f, 2f)]
        public float mineralBlobStrength = 0.9f;
        [Tooltip("Falloff power")]
        [Range(1f, 5f)]
        public float mineralFalloffPower = 2f;
        [Tooltip("Density threshold for mineral to be 'active'")]
        [Range(0f, 1f)]
        public float mineralThreshold = 0.3f;

        [Header("Ore Field (Asteroid-correlated heavy metals)")]
        [Tooltip("Minimum distance between ore blob centers")]
        public float oreBlobSpacing = 100000f;
        [Tooltip("Minimum ore blob radius")]
        public float oreBlobRadiusMin = 30000f;
        [Tooltip("Maximum ore blob radius")]
        public float oreBlobRadiusMax = 120000f;
        [Tooltip("Blob strength")]
        [Range(0.1f, 2f)]
        public float oreBlobStrength = 0.85f;
        [Tooltip("Falloff power")]
        [Range(1f, 5f)]
        public float oreFalloffPower = 2.5f;
        [Tooltip("Density threshold for ore to be 'active'")]
        [Range(0f, 1f)]
        public float oreThreshold = 0.3f;

        [Header("Gas Field (Nebula-correlated gaseous resources)")]
        [Tooltip("Minimum distance between gas blob centers")]
        public float gasBlobSpacing = 180000f;
        [Tooltip("Minimum gas blob radius")]
        public float gasBlobRadiusMin = 60000f;
        [Tooltip("Maximum gas blob radius")]
        public float gasBlobRadiusMax = 250000f;
        [Tooltip("Blob strength")]
        [Range(0.1f, 2f)]
        public float gasBlobStrength = 0.9f;
        [Tooltip("Falloff power")]
        [Range(1f, 5f)]
        public float gasFalloffPower = 2f;
        [Tooltip("Density threshold for gas to be 'active'")]
        [Range(0f, 1f)]
        public float gasThreshold = 0.3f;

        [Header("Exotic Field (Anomaly-correlated rare materials)")]
        [Tooltip("Minimum distance between exotic blob centers")]
        public float exoticBlobSpacing = 400000f;
        [Tooltip("Minimum exotic blob radius")]
        public float exoticBlobRadiusMin = 15000f;
        [Tooltip("Maximum exotic blob radius")]
        public float exoticBlobRadiusMax = 50000f;
        [Tooltip("Blob strength")]
        [Range(0.1f, 2f)]
        public float exoticBlobStrength = 0.85f;
        [Tooltip("Falloff power")]
        [Range(1f, 5f)]
        public float exoticFalloffPower = 3f;
        [Tooltip("Density threshold for exotic to be 'active'")]
        [Range(0f, 1f)]
        public float exoticThreshold = 0.4f;

        [Header("Water Field (General, slight nebula boost)")]
        [Tooltip("Minimum distance between water blob centers")]
        public float waterBlobSpacing = 150000f;
        [Tooltip("Minimum water blob radius")]
        public float waterBlobRadiusMin = 50000f;
        [Tooltip("Maximum water blob radius")]
        public float waterBlobRadiusMax = 200000f;
        [Tooltip("Blob strength")]
        [Range(0.1f, 2f)]
        public float waterBlobStrength = 0.9f;
        [Tooltip("Falloff power")]
        [Range(1f, 5f)]
        public float waterFalloffPower = 2f;
        [Tooltip("Density threshold for water to be 'active'")]
        [Range(0f, 1f)]
        public float waterThreshold = 0.3f;

        [Header("Zone Correlation Multipliers")]
        [Tooltip("How much asteroid zones boost mineral density")]
        [Range(1f, 3f)]
        public float mineralAsteroidBoost = 1.5f;
        [Tooltip("How much asteroid zones boost ore density")]
        [Range(1f, 3f)]
        public float oreAsteroidBoost = 1.4f;
        [Tooltip("How much nebula zones boost gas density")]
        [Range(1f, 3f)]
        public float gasNebulaBoost = 1.5f;
        [Tooltip("How much anomaly zones boost exotic density")]
        [Range(1f, 3f)]
        public float exoticAnomalyBoost = 1.8f;
        [Tooltip("How much nebula zones boost water density")]
        [Range(1f, 3f)]
        public float waterNebulaBoost = 1.2f;

        [Header("Rarity Tier Thresholds")]
        [Tooltip("Overall resource value above which tier is Common")]
        [Range(0f, 1f)]
        public float commonThreshold = 0.1f;
        [Tooltip("Overall resource value above which tier is Uncommon")]
        [Range(0f, 1f)]
        public float uncommonThreshold = 0.3f;
        [Tooltip("Overall resource value above which tier is Rare")]
        [Range(0f, 1f)]
        public float rareThreshold = 0.55f;
        [Tooltip("Overall resource value above which tier is Exotic")]
        [Range(0f, 1f)]
        public float exoticTierThreshold = 0.75f;

        [Header("Resource Value Weights (for OverallResourceValue)")]
        [Tooltip("Weight of mineral in overall value calculation")]
        public float mineralValueWeight = 1f;
        [Tooltip("Weight of ore in overall value calculation")]
        public float oreValueWeight = 1.2f;
        [Tooltip("Weight of gas in overall value calculation")]
        public float gasValueWeight = 1.5f;
        [Tooltip("Weight of exotic in overall value calculation")]
        public float exoticValueWeight = 3f;
        [Tooltip("Weight of water in overall value calculation")]
        public float waterValueWeight = 0.8f;

        public MetaballFieldConfig GetMineralFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = mineralBlobSpacing,
            blobRadiusMin = mineralBlobRadiusMin,
            blobRadiusMax = mineralBlobRadiusMax,
            blobStrength = mineralBlobStrength,
            falloffPower = mineralFalloffPower,
            threshold = mineralThreshold,
        };

        public MetaballFieldConfig GetOreFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = oreBlobSpacing,
            blobRadiusMin = oreBlobRadiusMin,
            blobRadiusMax = oreBlobRadiusMax,
            blobStrength = oreBlobStrength,
            falloffPower = oreFalloffPower,
            threshold = oreThreshold,
        };

        public MetaballFieldConfig GetGasFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = gasBlobSpacing,
            blobRadiusMin = gasBlobRadiusMin,
            blobRadiusMax = gasBlobRadiusMax,
            blobStrength = gasBlobStrength,
            falloffPower = gasFalloffPower,
            threshold = gasThreshold,
        };

        public MetaballFieldConfig GetExoticFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = exoticBlobSpacing,
            blobRadiusMin = exoticBlobRadiusMin,
            blobRadiusMax = exoticBlobRadiusMax,
            blobStrength = exoticBlobStrength,
            falloffPower = exoticFalloffPower,
            threshold = exoticThreshold,
        };

        public MetaballFieldConfig GetWaterFieldConfig() => new MetaballFieldConfig
        {
            blobSpacing = waterBlobSpacing,
            blobRadiusMin = waterBlobRadiusMin,
            blobRadiusMax = waterBlobRadiusMax,
            blobStrength = waterBlobStrength,
            falloffPower = waterFalloffPower,
            threshold = waterThreshold,
        };
    }
}
