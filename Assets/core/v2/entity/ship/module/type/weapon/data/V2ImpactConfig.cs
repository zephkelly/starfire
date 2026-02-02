using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for projectile impact effects (visual and audio).
    /// </summary>
    [Serializable]
    public class V2ImpactConfig
    {
        [Header("Particle Effects")]
        [Tooltip("Particle system prefab to spawn on impact.")]
        public GameObject impactParticlePrefab;

        [Tooltip("Scale multiplier for impact particles.")]
        public float particleScale = 1f;

        [Header("Lighting")]
        [Tooltip("Whether to spawn a point light on impact.")]
        public bool spawnLight = true;

        [Tooltip("Color of the impact light.")]
        public Color lightColor = Color.white;

        [Tooltip("Intensity of the impact light.")]
        public float lightIntensity = 1f;

        [Tooltip("Radius of the impact light.")]
        public float lightRadius = 2f;

        [Tooltip("Duration the light remains visible.")]
        public float lightDuration = 0.2f;

        [Header("Audio")]
        [Tooltip("Audio clip to play on impact.")]
        public AudioClip impactSound;

        [Tooltip("Volume of impact sound.")]
        [Range(0f, 1f)]
        public float soundVolume = 1f;

        [Header("Timing")]
        [Tooltip("How long impact effects persist before cleanup.")]
        public float effectDuration = 1f;

        [Header("Preset System")]
        [Tooltip("Impact preset (new system). When set, overrides impactParticlePrefab and light settings.")]
        public ImpactPreset impactPreset;

        [Header("Screen Shake")]
        [Tooltip("Configuration for camera screen shake on impact.")]
        public V2ScreenShakeConfig screenShakeConfig;

        /// <summary>
        /// Default impact configuration with minimal effects.
        /// </summary>
        public static V2ImpactConfig Default => new V2ImpactConfig
        {
            particleScale = 1f,
            spawnLight = true,
            lightColor = Color.white,
            lightIntensity = 1f,
            lightRadius = 2f,
            lightDuration = 0.2f,
            soundVolume = 1f,
            effectDuration = 1f
        };
    }
}
