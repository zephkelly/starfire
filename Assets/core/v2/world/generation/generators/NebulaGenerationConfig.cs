using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Regions;

namespace Starfire.Core.V2.World.Generation.Generators
{
    /// <summary>
    /// ScriptableObject configuration for nebula generation.
    /// Defines how nebulas are distributed and what types appear.
    /// </summary>
    [CreateAssetMenu(fileName = "NebulaGenerationConfig", menuName = "Starfire/World/Nebula Generation Config")]
    public class NebulaGenerationConfig : ScriptableObject
    {
        [Header("General")]
        [Tooltip("Enable nebula generation")]
        public bool enabled = true;

        [Header("Density Field")]
        [Tooltip("Scale of the noise used for nebula density (larger = bigger regions)")]
        [Range(0.0001f, 0.01f)]
        public float densityNoiseScale = 0.001f;

        [Tooltip("Minimum density value to spawn any nebulae (0-1)")]
        [Range(0f, 1f)]
        public float minDensityThreshold = 0.3f;

        [Tooltip("Offset for density noise sampling")]
        public Vector2 densityNoiseOffset = Vector2.zero;

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

        [Tooltip("Distribution curve for radius (0=min, 1=max). Use to bias towards smaller or larger nebulae.")]
        public AnimationCurve radiusDistribution = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Nebula Configurations")]
        [Tooltip("Available nebula configurations to choose from")]
        public List<WeightedNebulaConfig> nebulaConfigs = new List<WeightedNebulaConfig>();

        [Serializable]
        public class WeightedNebulaConfig
        {
            [Tooltip("The nebula region configuration")]
            public NebulaRegionConfig config;

            [Tooltip("Relative weight for selection (higher = more common)")]
            [Range(0f, 10f)]
            public float weight = 1f;

            [Tooltip("Minimum density required to spawn this config type")]
            [Range(0f, 1f)]
            public float minDensity = 0f;

            [Tooltip("Maximum density for this config (use 1 for no limit)")]
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
        /// Select a random config based on weights and density.
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
        /// Sample nebula radius using the distribution curve.
        /// </summary>
        public float SampleRadius(System.Random rng)
        {
            float t = (float)rng.NextDouble();
            float curved = radiusDistribution.Evaluate(t);
            return Mathf.Lerp(minRadius, maxRadius, curved);
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
