using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    [CreateAssetMenu(fileName = "CelestialBodyConfig", menuName = "Starfire/World Fabric/Celestial Body Config")]
    public class CelestialBodyConfig : ScriptableObject
    {
        [Header("General")]
        public bool enabled = true;

        [Header("Star Placement")]
        [Tooltip("Distance between star grid cells. Controls how far apart stars are.")]
        public float starSpacing = 500000f;

        [Tooltip("Chance of a star spawning in each grid cell (0-1)")]
        [Range(0f, 1f)]
        public float starSpawnChance = 0.7f;

        [Tooltip("Star type definitions with spawn weights")]
        public List<StarTypeConfig> starTypes = new List<StarTypeConfig>
        {
            new StarTypeConfig { type = StarType.RedDwarf, color = new Color(1f, 0.4f, 0.3f), radiusRange = new Vector2(300f, 800f), massRange = new Vector2(500f, 5000f), luminosity = 0.3f, spawnWeight = 0.5f },
            new StarTypeConfig { type = StarType.YellowStar, color = new Color(1f, 0.95f, 0.7f), radiusRange = new Vector2(800f, 2000f), massRange = new Vector2(5000f, 30000f), luminosity = 1f, spawnWeight = 0.3f },
            new StarTypeConfig { type = StarType.BlueGiant, color = new Color(0.6f, 0.7f, 1f), radiusRange = new Vector2(2000f, 5000f), massRange = new Vector2(30000f, 100000f), luminosity = 3f, spawnWeight = 0.08f },
            new StarTypeConfig { type = StarType.WhiteDwarf, color = new Color(0.9f, 0.9f, 1f), radiusRange = new Vector2(100f, 400f), massRange = new Vector2(3000f, 20000f), luminosity = 0.5f, spawnWeight = 0.07f },
            new StarTypeConfig { type = StarType.Neutron, color = new Color(0.8f, 0.9f, 1f), radiusRange = new Vector2(50f, 200f), massRange = new Vector2(50000f, 200000f), luminosity = 0.1f, spawnWeight = 0.05f },
        };

        [Header("Planet Placement")]
        [Tooltip("Min/max planets per star system")]
        public Vector2Int planetsPerStarRange = new Vector2Int(1, 8);

        [Tooltip("Min/max orbit radius from parent star")]
        public Vector2 planetOrbitRadiusRange = new Vector2(10000f, 200000f);

        [Tooltip("Minimum angular separation between planets (degrees)")]
        [Range(10f, 90f)]
        public float minPlanetAngularSeparation = 20f;

        [Tooltip("Planet type definitions with spawn weights")]
        public List<PlanetTypeConfig> planetTypes = new List<PlanetTypeConfig>
        {
            new PlanetTypeConfig { type = PlanetType.Rocky, color = new Color(0.6f, 0.5f, 0.4f), radiusRange = new Vector2(50f, 400f), massRange = new Vector2(10f, 500f), spawnWeight = 0.35f, ringChance = 0.02f, parallaxRange = new Vector2(0.05f, 0.2f) },
            new PlanetTypeConfig { type = PlanetType.GasGiant, color = new Color(0.8f, 0.6f, 0.4f), radiusRange = new Vector2(500f, 2000f), massRange = new Vector2(1000f, 10000f), spawnWeight = 0.2f, ringChance = 0.4f, parallaxRange = new Vector2(0.3f, 0.6f) },
            new PlanetTypeConfig { type = PlanetType.IceGiant, color = new Color(0.5f, 0.7f, 0.9f), radiusRange = new Vector2(300f, 1200f), massRange = new Vector2(500f, 5000f), spawnWeight = 0.15f, ringChance = 0.25f, parallaxRange = new Vector2(0.2f, 0.45f) },
            new PlanetTypeConfig { type = PlanetType.Molten, color = new Color(0.9f, 0.3f, 0.1f), radiusRange = new Vector2(80f, 500f), massRange = new Vector2(20f, 800f), spawnWeight = 0.15f, ringChance = 0f, parallaxRange = new Vector2(0.05f, 0.15f) },
            new PlanetTypeConfig { type = PlanetType.Barren, color = new Color(0.5f, 0.5f, 0.5f), radiusRange = new Vector2(40f, 300f), massRange = new Vector2(5f, 300f), spawnWeight = 0.15f, ringChance = 0.01f, parallaxRange = new Vector2(0.03f, 0.12f) },
        };

        [Header("Planetary Rings")]
        [Tooltip("Ring inner radius as multiplier of planet radius")]
        [Range(1.5f, 3f)]
        public float ringInnerRadiusMult = 1.8f;

        [Tooltip("Ring outer radius as multiplier of planet radius")]
        [Range(2f, 6f)]
        public float ringOuterRadiusMult = 3.5f;

        [Tooltip("Asteroid density within rings (affects asteroid generator)")]
        [Range(0f, 1f)]
        public float ringAsteroidDensity = 0.7f;

        [Header("Gravity")]
        [Tooltip("Multiplier for gravity radius relative to body radius")]
        public float gravityRadiusMultiplier = 20f;

        /// <summary>
        /// Select a star type based on weighted random using a hash value 0-1.
        /// </summary>
        public StarTypeConfig SelectStarType(float roll)
        {
            return SelectWeighted(starTypes, roll, s => s.spawnWeight);
        }

        /// <summary>
        /// Select a planet type based on weighted random using a hash value 0-1.
        /// </summary>
        public PlanetTypeConfig SelectPlanetType(float roll)
        {
            return SelectWeighted(planetTypes, roll, p => p.spawnWeight);
        }

        private static T SelectWeighted<T>(List<T> items, float roll, System.Func<T, float> getWeight)
        {
            if (items == null || items.Count == 0) return default;

            float totalWeight = 0f;
            foreach (var item in items)
                totalWeight += getWeight(item);

            float threshold = roll * totalWeight;
            float cumulative = 0f;
            foreach (var item in items)
            {
                cumulative += getWeight(item);
                if (threshold <= cumulative)
                    return item;
            }

            return items[items.Count - 1];
        }
    }
}
