using UnityEngine;
using Starfire.Core.Background.Layers;

namespace Starfire.Core.Background.Presets
{
    /// <summary>
    /// ScriptableObject preset for CometLayer configuration.
    /// Assign to a CometLayer to override all its settings.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCometPreset", menuName = "Starfire/Presets/Comet Preset")]
    public class CometLayerPreset : ScriptableObject
    {
        [Header("Layer Settings")]
        [Tooltip("Parallax depth - lower values = farther/slower")]
        public float parallaxDepth = 0.02f;

        [Header("Spawning")]
        [Tooltip("Average seconds between spawn attempts")]
        public float spawnInterval = 15f;

        [Tooltip("Random variance in spawn timing")]
        public float spawnIntervalVariance = 10f;

        [Tooltip("Maximum simultaneous comets")]
        [Range(1, 4)]
        public int maxActiveComets = 3;

        [Header("Movement")]
        [Tooltip("Base speed in world units per second (slower than shooting stars)")]
        public float speed = 8f;

        [Tooltip("Random speed variance")]
        public float speedVariance = 4f;

        [Header("Trail")]
        [Tooltip("How many seconds of trail to show behind the comet")]
        [Range(0.1f, 2f)]
        public float trailTime = 0.5f;

        [Tooltip("Reference parallax depth for trail scaling (deeper layers get shorter trails)")]
        public float referenceParallaxDepth = 0.02f;

        [Header("Direction Noise")]
        [Tooltip("Perlin noise scale (smaller = larger regions with same direction)")]
        public float noiseScale = 0.05f;

        [Tooltip("How much noise affects direction (0-1)")]
        [Range(0f, 1f)]
        public float noiseInfluence = 0.5f;

        [Tooltip("Base direction in degrees (0=right, 90=up, 225=down-left)")]
        public float baseAngle = 225f;

        [Tooltip("Random angle variance on top of noise")]
        public float angleVariance = 30f;

        [Header("Nucleus")]
        [Tooltip("Size of the bright nucleus core")]
        [Min(0.01f)]
        public float nucleusSize = 0.15f;

        [Tooltip("Size of the fuzzy coma glow")]
        [Min(0.01f)]
        public float comaSize = 0.6f;

        [Tooltip("How soft the coma edge is (higher = softer)")]
        [Range(0.5f, 5f)]
        public float comaSoftness = 2f;

        [Header("Particle Trail")]
        [Tooltip("Number of particles per comet trail")]
        [Range(8, 48)]
        public int particleCount = 24;

        [Tooltip("Minimum particle size")]
        [Min(0.005f)]
        public float particleSizeMin = 0.02f;

        [Tooltip("Maximum particle size")]
        [Min(0.01f)]
        public float particleSizeMax = 0.08f;

        [Tooltip("How wide particles spread from center line")]
        [Min(0f)]
        public float particleSpread = 0.5f;

        [Tooltip("How quickly particles dim along trail (higher = faster fade)")]
        [Range(0.5f, 5f)]
        public float particleFadeRate = 2f;

        [Header("Appearance")]
        public Color cometColor = new Color(0.8f, 0.9f, 1f, 1f);

        [Tooltip("Base brightness of the comet")]
        [Range(0.5f, 8f)]
        public float brightness = 3f;

        [Header("Pixelization")]
        [Tooltip("Resolution for pixel-art effect (higher = more pixels)")]
        [Range(100f, 1000f)]
        public float pixels = 400f;

        [Header("Gradient")]
        [Tooltip("1D gradient texture for color banding (hot->cold left to right)")]
        public Texture2D gradientTexture;

        [Header("Sun Direction")]
        [Tooltip("Direction light comes FROM (normalized). Ion tail points opposite.")]
        public Vector2 sunDirection = new Vector2(-1f, -0.5f);

        [Header("Dual Tails")]
        [Tooltip("How much the dust tail curves perpendicular to travel")]
        [Range(0f, 1f)]
        public float dustTailCurve = 0.3f;

        [Tooltip("Width of the dust tail")]
        [Range(0.1f, 2f)]
        public float dustTailWidth = 0.8f;

        [Tooltip("Dust tail falloff curve")]
        [Range(1f, 5f)]
        public float dustFalloff = 2f;

        [Tooltip("Ion tail length relative to dust tail")]
        [Range(0.5f, 2f)]
        public float ionTailLengthMultiplier = 1.5f;

        [Tooltip("Width of the ion tail")]
        [Range(0.05f, 0.5f)]
        public float ionTailWidth = 0.2f;

        [Tooltip("Ion tail falloff curve")]
        [Range(1f, 5f)]
        public float ionFalloff = 1.5f;

        [Header("Tail Noise")]
        [Tooltip("Scale of FBM noise on tail edges")]
        public float tailNoiseScale = 50f;

        [Tooltip("FBM octaves for tail edge noise")]
        [Range(1, 5)]
        public int tailNoiseOctaves = 3;

        [Header("Core Animation")]
        [Tooltip("Speed of nucleus brightness pulsing")]
        [Range(0.5f, 5f)]
        public float corePulseSpeed = 1.5f;

        [Tooltip("Amount of nucleus size/brightness variation")]
        [Range(0f, 0.5f)]
        public float corePulseAmount = 0.15f;

        [Header("Boiling Front")]
        [Tooltip("Cell scale for boiling effect")]
        [Range(2f, 12f)]
        public float boilCellScale = 6f;

        [Tooltip("Animation speed of boiling effect")]
        [Range(0.5f, 5f)]
        public float boilSpeed = 2f;

        [Tooltip("Intensity of the boiling effect")]
        [Range(0f, 1f)]
        public float boilIntensity = 0.6f;

        [Header("Sparkles")]
        [Tooltip("Number of sparkle particles per comet")]
        [Range(8, 32)]
        public int sparkleCount = 16;

        [Tooltip("Size of sparkle particles")]
        [Range(0.005f, 0.03f)]
        public float sparkleSize = 0.015f;

        [Tooltip("Sparkle twinkle animation speed")]
        [Range(2f, 15f)]
        public float sparkleSpeed = 8f;

        [Tooltip("Brightness of sparkle particles")]
        [Range(0.5f, 3f)]
        public float sparkleBrightness = 1.5f;

        [Header("Seed")]
        [Tooltip("Seed for deterministic noise patterns")]
        [Range(1, 10)]
        public int seed = 1;

        /// <summary>
        /// Apply all preset values to the given layer.
        /// </summary>
        public void ApplyTo(CometLayer layer)
        {
            layer.parallaxDepth = parallaxDepth;
            layer.spawnInterval = spawnInterval;
            layer.spawnIntervalVariance = spawnIntervalVariance;
            layer.maxActiveComets = maxActiveComets;
            layer.speed = speed;
            layer.speedVariance = speedVariance;
            layer.trailTime = trailTime;
            layer.referenceParallaxDepth = referenceParallaxDepth;
            layer.noiseScale = noiseScale;
            layer.noiseInfluence = noiseInfluence;
            layer.baseAngle = baseAngle;
            layer.angleVariance = angleVariance;
            layer.nucleusSize = nucleusSize;
            layer.comaSize = comaSize;
            layer.comaSoftness = comaSoftness;
            layer.particleCount = particleCount;
            layer.particleSizeMin = particleSizeMin;
            layer.particleSizeMax = particleSizeMax;
            layer.particleSpread = particleSpread;
            layer.particleFadeRate = particleFadeRate;
            layer.cometColor = cometColor;
            layer.brightness = brightness;
            layer.pixels = pixels;
            layer.gradientTexture = gradientTexture;
            layer.sunDirection = sunDirection;
            layer.dustTailCurve = dustTailCurve;
            layer.dustTailWidth = dustTailWidth;
            layer.dustFalloff = dustFalloff;
            layer.ionTailLengthMultiplier = ionTailLengthMultiplier;
            layer.ionTailWidth = ionTailWidth;
            layer.ionFalloff = ionFalloff;
            layer.tailNoiseScale = tailNoiseScale;
            layer.tailNoiseOctaves = tailNoiseOctaves;
            layer.corePulseSpeed = corePulseSpeed;
            layer.corePulseAmount = corePulseAmount;
            layer.boilCellScale = boilCellScale;
            layer.boilSpeed = boilSpeed;
            layer.boilIntensity = boilIntensity;
            layer.sparkleCount = sparkleCount;
            layer.sparkleSize = sparkleSize;
            layer.sparkleSpeed = sparkleSpeed;
            layer.sparkleBrightness = sparkleBrightness;
            layer.seed = seed;
        }
    }
}
