using UnityEngine;

namespace Starfire.Core.V2.World.Generation.Generators
{
    [CreateAssetMenu(fileName = "AsteroidGenerationConfig", menuName = "Starfire/World Generation/Asteroid Generation Config")]
    public class AsteroidGenerationConfig : ScriptableObject
    {
        [Header("General")]
        public bool enabled = true;

        [Header("Belt Asteroids (from SpaceZoneLayer asteroid density)")]
        [Tooltip("Minimum spacing between belt asteroids")]
        public float beltMinSpacing = 200f;

        [Tooltip("Multiplier for asteroid density from fabric sample")]
        [Range(0.1f, 5f)]
        public float beltDensityMultiplier = 1f;

        [Tooltip("Minimum fabric asteroid density to start spawning")]
        [Range(0f, 1f)]
        public float beltMinDensityThreshold = 0.15f;

        [Tooltip("Size range for belt asteroids")]
        public Vector2 beltSizeRange = new Vector2(5f, 50f);

        [Header("Ring Asteroids (around ringed planets)")]
        [Tooltip("Number of asteroids per ring segment")]
        [Range(1, 20)]
        public int ringAsteroidsPerSegment = 8;

        [Tooltip("Number of angular segments around a ring")]
        [Range(8, 128)]
        public int ringSegments = 64;

        [Tooltip("Size range for ring asteroids")]
        public Vector2 ringSizeRange = new Vector2(2f, 20f);

        [Header("Scatter Asteroids (sparse open space)")]
        [Tooltip("Chance of scatter asteroids appearing in a chunk")]
        [Range(0f, 1f)]
        public float scatterChance = 0.3f;

        [Tooltip("Number of scatter asteroids when present")]
        public Vector2Int scatterCountRange = new Vector2Int(1, 5);

        [Tooltip("Size range for scattered asteroids")]
        public Vector2 scatterSizeRange = new Vector2(3f, 30f);

        [Header("Belt Organic Variation")]
        [Tooltip("Noise scale for position jitter (smaller = larger clumps)")]
        public float jitterNoiseScale = 0.01f;

        [Tooltip("Maximum jitter displacement in units")]
        public float jitterAmount = 80f;

        [Tooltip("Noise scale for density variation within belts")]
        public float densityNoiseScale = 0.005f;

        [Tooltip("How much noise affects local density (0=uniform, 1=full variation)")]
        [Range(0f, 1f)]
        public float densityNoiseInfluence = 0.6f;

        [Header("Ring Organic Variation")]
        [Tooltip("Probability threshold for ring segment culling (creates gaps like Cassini division)")]
        [Range(0f, 0.8f)]
        public float ringGapThreshold = 0.25f;

        [Tooltip("Noise strength for radial ring displacement (fraction of ring width)")]
        [Range(0f, 0.5f)]
        public float ringRadialNoise = 0.25f;

        [Header("Visuals")]
        [Tooltip("Number of visual variants")]
        [Range(1, 10)]
        public int variantCount = 4;

        [Tooltip("Prefabs for each asteroid variant. Index maps to AsteroidDefinition.Variant.")]
        public GameObject[] variantPrefabs;
    }
}
