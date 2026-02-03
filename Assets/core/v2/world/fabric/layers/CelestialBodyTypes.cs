using System;
using UnityEngine;
using Starfire.Core.V2.World;

namespace StarfireV2
{
    public enum StarType
    {
        RedDwarf,
        YellowStar,
        BlueGiant,
        WhiteDwarf,
        Neutron
    }

    public enum PlanetType
    {
        Rocky,
        GasGiant,
        IceGiant,
        Molten,
        Barren
    }

    public enum CelestialBodyKind
    {
        Star,
        Planet
    }

    /// <summary>
    /// Describes a single celestial body placed by the CelestialBodyLayer.
    /// Deterministically generated from spatial hash, queryable at any position.
    /// </summary>
    public struct CelestialBodyInfo
    {
        public CelestialBodyKind Kind;
        public Vector2D AbsolutePosition;
        public float Radius;
        public float Mass;
        public float GravityRadius;
        public float Seed;

        // Star-specific
        public StarType StarType;
        public float Luminosity;

        // Planet-specific
        public PlanetType PlanetType;
        public Vector2D ParentStarPosition;
        public float OrbitRadius;
        public float OrbitAngle;
        public float ParallaxDepthFactor;

        // Ring
        public bool HasRing;
        public float RingInnerRadius;
        public float RingOuterRadius;
    }

    [Serializable]
    public class StarTypeConfig
    {
        public StarType type;
        public Color color = Color.white;
        [MinMax(100f, 50000f)]
        public Vector2 radiusRange = new Vector2(500f, 5000f);
        public Vector2 massRange = new Vector2(1000f, 100000f);
        public float luminosity = 1f;
        [Range(0f, 1f)]
        public float spawnWeight = 1f;

        [Header("Visuals")]
        public GameObject prefab;
    }

    [Serializable]
    public class PlanetTypeConfig
    {
        public PlanetType type;
        public Color color = Color.gray;
        public Vector2 radiusRange = new Vector2(50f, 2000f);
        public Vector2 massRange = new Vector2(10f, 10000f);
        [Range(0f, 1f)]
        public float spawnWeight = 1f;
        [Range(0f, 1f)]
        public float ringChance = 0.1f;
        [Tooltip("Parallax depth range: larger values = more depth effect = appears bigger")]
        public Vector2 parallaxRange = new Vector2(0.1f, 0.5f);

        [Header("Visuals")]
        public GameObject prefab;
        [Tooltip("Prefab used for ringed variant of this planet type. Falls back to prefab if null.")]
        public GameObject ringedPrefab;
    }

    /// <summary>
    /// Attribute placeholder for Vector2 min/max display.
    /// </summary>
    public class MinMaxAttribute : PropertyAttribute
    {
        public float Min;
        public float Max;
        public MinMaxAttribute(float min, float max) { Min = min; Max = max; }
    }
}
