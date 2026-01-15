using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Background.Layers;
using Starfire.Core.Background.Behaviors;

namespace Starfire.Core.Background.Presets
{
    /// <summary>
    /// ScriptableObject preset for ShootingStarLayer configuration.
    /// Assign to a ShootingStarLayer to override all its settings.
    /// Note: Behavior configs are referenced, not copied, so they remain editable.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShootingStarPreset", menuName = "Starfire/Presets/Shooting Star Preset")]
    public class ShootingStarLayerPreset : ScriptableObject
    {
        [Header("Layer Settings")]
        [Tooltip("Parallax depth - lower values = farther/slower")]
        public float parallaxDepth = 0.1f;

        [Header("Spawning")]
        [Tooltip("Average seconds between spawn attempts")]
        public float spawnInterval = 3f;

        [Tooltip("Random variance in spawn timing")]
        public float spawnIntervalVariance = 2f;

        [Tooltip("Enable to limit the number of simultaneous shooting stars")]
        public bool limitActiveStars = true;

        [Tooltip("Maximum simultaneous shooting stars (only used when limitActiveStars is enabled)")]
        [Range(1, 264)]
        public int maxActiveStars = 5;

        [Tooltip("Spawn margin multiplier - how far outside viewport stars spawn (1.0 = viewport edge)")]
        [Min(1.0f)]
        public float spawnMargin = 1.2f;

        [Header("Movement")]
        [Tooltip("Minimum visual speed in shader units per second")]
        [Min(0.01f)]
        public float speedMin = 8.00f;

        [Tooltip("Maximum visual speed in shader units per second")]
        [Min(0.01f)]
        public float speedMax = 22.00f;

        [Tooltip("Speed distribution curve (X=random 0-1, Y=lerp factor between min/max)")]
        public AnimationCurve speedDistribution = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Visual trail length as fraction of screen height (0.15 = 15%)")]
        public float trailLength = 0.15f;

        [Tooltip("Random trail length variance")]
        public float trailLengthVariance = 0.05f;

        [Header("Direction Noise")]
        [Tooltip("Perlin noise scale (smaller = larger regions with same direction)")]
        public float noiseScale = 0.05f;

        [Tooltip("How much noise affects direction (0-1)")]
        [Range(0f, 1f)]
        public float noiseInfluence = 0.5f;

        [Tooltip("Random angle variance applied to the toward-viewport direction")]
        public float angleVariance = 30f;

        [Header("Appearance")]
        public Color starColor = Color.white;

        [Tooltip("Base brightness of the shooting star")]
        [Range(0.2f, 5f)]
        public float brightness = 1.5f;

        [Tooltip("Random brightness variance (0 = all same brightness)")]
        [Range(0f, 1f)]
        public float brightnessVariance = 0.3f;

        [Tooltip("Exponent for speed-to-brightness curve (1=linear, <1=brighter slow stars, >1=dimmer slow stars)")]
        [Range(0.5f, 3f)]
        public float speedBrightnessCurve = 1f;

        [Tooltip("Minimum width as fraction of screen height (0.01 = 1%)")]
        [Min(0.001f)]
        public float starWidthMin = 0.01f;

        [Tooltip("Maximum width as fraction of screen height (0.03 = 3%)")]
        [Min(0.001f)]
        public float starWidthMax = 0.03f;

        [Header("Behavior Configuration")]
        [Tooltip("List of behavior configs with selection weights. Leave empty to use default behavior.")]
        public List<ShootingStarBehaviorConfig> behaviorConfigs = new List<ShootingStarBehaviorConfig>();

        [Tooltip("Lifetime multiplier for Persistent behavior stars")]
        [Min(1f)]
        public float persistentLifetimeMultiplier = 3f;

        [Header("Kill Zone")]
        [Tooltip("Multiplier for kill zone rectangle (relative to spawn margin). Stars beyond this are forcibly removed.")]
        [Min(1.0f)]
        public float killZoneMargin = 2.0f;

        [Header("Spawn Validation")]
        [Tooltip("Minimum dot product between direction and inward normal (0 = perpendicular allowed, 1 = must point directly inward)")]
        [Range(0f, 0.5f)]
        public float minimumInwardComponent = 0.1f;

        [Tooltip("Maximum spawn attempts before giving up")]
        [Range(1, 10)]
        public int maxSpawnAttempts = 5;

        /// <summary>
        /// Apply all preset values to the given layer.
        /// Note: This uses reflection to set private serialized fields.
        /// </summary>
        public void ApplyTo(ShootingStarLayer layer)
        {
            layer.parallaxDepth = parallaxDepth;
            layer.spawnInterval = spawnInterval;
            layer.spawnIntervalVariance = spawnIntervalVariance;
            layer.limitActiveStars = limitActiveStars;
            layer.maxActiveStars = maxActiveStars;
            layer.spawnMargin = spawnMargin;
            layer.speedMin = speedMin;
            layer.speedMax = speedMax;
            layer.speedDistribution = new AnimationCurve(speedDistribution.keys);
            layer.trailLength = trailLength;
            layer.trailLengthVariance = trailLengthVariance;
            layer.noiseScale = noiseScale;
            layer.noiseInfluence = noiseInfluence;
            layer.angleVariance = angleVariance;
            layer.starColor = starColor;
            layer.brightness = brightness;
            layer.brightnessVariance = brightnessVariance;
            layer.speedBrightnessCurve = speedBrightnessCurve;
            layer.starWidthMin = starWidthMin;
            layer.starWidthMax = starWidthMax;
            layer.killZoneMargin = killZoneMargin;
            layer.minimumInwardComponent = minimumInwardComponent;
            layer.maxSpawnAttempts = maxSpawnAttempts;

            // Apply behavior configs using reflection since it's a private serialized field
            var behaviorField = typeof(ShootingStarLayer).GetField("behaviorConfigs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (behaviorField != null)
            {
                var targetList = behaviorField.GetValue(layer) as List<ShootingStarBehaviorConfig>;
                if (targetList != null)
                {
                    targetList.Clear();
                    targetList.AddRange(behaviorConfigs);
                }
            }

            var multiplierField = typeof(ShootingStarLayer).GetField("persistentLifetimeMultiplier",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (multiplierField != null)
            {
                multiplierField.SetValue(layer, persistentLifetimeMultiplier);
            }
        }
    }
}
