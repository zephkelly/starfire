using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Regions;

namespace Starfire.Core.V2.World.Generation.Generators
{
    /// <summary>
    /// Configuration for fabric-integrated nebula generation.
    /// Uses SpaceZoneLayer data to determine where nebulas spawn,
    /// ensuring visual nebulas align with NebulaDense zones.
    /// </summary>
    [CreateAssetMenu(fileName = "FabricNebulaGenerationConfig", menuName = "Starfire/World/Fabric Nebula Generation Config")]
    public class FabricNebulaGenerationConfig : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Enable fabric-integrated nebula generation")]
        public bool enabled = true;

        [Header("Zone Integration")]
        [Tooltip("Minimum zone density required to spawn nebulae (should match SpaceZoneConfig.nebulaThreshold)")]
        [Range(0f, 1f)]
        public float minZoneDensity = 0.65f;

        [Tooltip("Only spawn in NebulaDense zones (recommended)")]
        public bool requireNebulaDenseZone = true;

        [Header("Spacing")]
        [Tooltip("Minimum distance between nebula centers")]
        [Min(10f)]
        public float minNebulaSpacing = 100f;

        [Tooltip("Maximum attempts for Poisson disk sampling per point")]
        [Range(10, 100)]
        public int maxSamplingAttempts = 30;

        [Header("Size")]
        [Tooltip("Minimum nebula radius")]
        [Min(10f)]
        public float minRadius = 30f;

        [Tooltip("Maximum nebula radius")]
        [Min(20f)]
        public float maxRadius = 150f;

        [Tooltip("How much zone density affects radius (higher density = larger nebulae)")]
        [Range(0f, 3f)]
        public float densityRadiusMultiplier = 1.5f;

        [Tooltip("Distribution curve for radius (0=min, 1=max)")]
        public AnimationCurve radiusDistribution = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Spawn Probability")]
        [Tooltip("Base spawn probability multiplier (1 = full probability from density)")]
        [Range(0f, 2f)]
        public float spawnProbabilityMultiplier = 1f;

        [Tooltip("Minimum spawn probability even at threshold density")]
        [Range(0f, 1f)]
        public float minSpawnProbability = 0.2f;

        [Header("Nebula Configurations")]
        [Tooltip("Available nebula configurations to choose from based on zone density")]
        public List<WeightedNebulaConfig> nebulaConfigs = new List<WeightedNebulaConfig>();

        [Serializable]
        public class WeightedNebulaConfig
        {
            [Tooltip("The nebula region configuration")]
            public NebulaRegionConfig config;

            [Tooltip("Relative weight for selection (higher = more common)")]
            [Range(0f, 10f)]
            public float weight = 1f;

            [Tooltip("Minimum zone density for this config type")]
            [Range(0f, 1f)]
            public float minDensity = 0f;

            [Tooltip("Maximum zone density for this config (use 1 for no limit)")]
            [Range(0f, 1f)]
            public float maxDensity = 1f;
        }

        /// <summary>
        /// Get total weight of all configs that can spawn at the given density.
        /// </summary>
        public float GetTotalWeight(float density)
        {
            float total = 0f;
            foreach (var wc in nebulaConfigs)
            {
                if (wc.config != null && density >= wc.minDensity && density <= wc.maxDensity)
                {
                    total += wc.weight;
                }
            }
            return total;
        }

        /// <summary>
        /// Select a random config based on weights and zone density.
        /// </summary>
        public NebulaRegionConfig SelectConfig(System.Random rng, float density)
        {
            float totalWeight = GetTotalWeight(density);
            if (totalWeight <= 0f)
            {
                // Fallback to first valid config
                foreach (var wc in nebulaConfigs)
                {
                    if (wc.config != null)
                        return wc.config;
                }
                return null;
            }

            float roll = (float)rng.NextDouble() * totalWeight;
            float accumulated = 0f;

            foreach (var wc in nebulaConfigs)
            {
                if (wc.config == null) continue;
                if (density < wc.minDensity || density > wc.maxDensity) continue;

                accumulated += wc.weight;
                if (roll <= accumulated)
                {
                    return wc.config;
                }
            }

            // Fallback
            return nebulaConfigs.Count > 0 ? nebulaConfigs[0].config : null;
        }

        /// <summary>
        /// Calculate nebula radius based on zone density.
        /// Higher density zones produce larger nebulae.
        /// </summary>
        public float CalculateRadius(System.Random rng, float zoneDensity)
        {
            float t = (float)rng.NextDouble();
            float curved = radiusDistribution.Evaluate(t);

            // Scale by density - higher density = larger nebulae
            float normalizedDensity = Mathf.InverseLerp(minZoneDensity, 1f, zoneDensity);
            float densityScale = 1f + (normalizedDensity * densityRadiusMultiplier);

            return Mathf.Lerp(minRadius, maxRadius, curved) * Mathf.Min(densityScale, 1f + densityRadiusMultiplier);
        }

        /// <summary>
        /// Calculate spawn probability based on zone density.
        /// </summary>
        public float CalculateSpawnProbability(float zoneDensity)
        {
            float normalizedDensity = Mathf.InverseLerp(minZoneDensity, 1f, zoneDensity);
            float probability = Mathf.Lerp(minSpawnProbability, 1f, normalizedDensity);
            return probability * spawnProbabilityMultiplier;
        }

        private void OnValidate()
        {
            if (maxRadius < minRadius)
                maxRadius = minRadius + 10f;

            if (minNebulaSpacing < minRadius * 2)
                minNebulaSpacing = minRadius * 2;
        }
    }
}
