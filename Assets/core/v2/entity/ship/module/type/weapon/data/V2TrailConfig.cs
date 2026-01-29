using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Inline configuration for shader-based projectile trails.
    /// Eliminates need for separate trail prefabs.
    /// </summary>
    [Serializable]
    public class V2TrailConfig
    {
        [Tooltip("Enable shader-based trail rendering.")]
        public bool enabled = false;

        [Header("Size")]
        [Tooltip("Trail length multiplier. Higher values = longer trails.")]
        [Range(0.5f, 5f)]
        public float length = 1.5f;

        [Tooltip("Trail width. Set to 0 to auto-detect from projectile sprite.")]
        public float width = 0f;

        [Tooltip("Width multiplier when using auto-detection from sprite.")]
        [Range(0.1f, 2f)]
        public float widthMultiplier = 0.8f;

        [Tooltip("Time in seconds for the trail to grow from zero to full length.")]
        [Range(0f, 0.5f)]
        public float growTime = 0.1f;

        [Header("Appearance")]
        [Tooltip("Use the projectile's color for the trail.")]
        public bool useProjectileColor = true;

        [Tooltip("Trail color. Only used if useProjectileColor is false.")]
        public Color color = Color.white;

        [Tooltip("Glow intensity multiplier for additive blending.")]
        [Range(0.5f, 5f)]
        public float glowIntensity = 1.5f;

        [Tooltip("Fade curve power. 1 = linear, 2 = quadratic (faster fade), 0.5 = slower fade.")]
        [Range(0.5f, 4f)]
        public float falloffPower = 2f;

        [Tooltip("Edge softness for smoother trail edges.")]
        [Range(0f, 1f)]
        public float softness = 0.3f;

        [Header("Advanced")]
        [Tooltip("Custom material to override the default trail shader.")]
        public Material customMaterial;

        [Tooltip("Sorting order offset relative to projectile sprite.")]
        public int sortingOrderOffset = -1;

        [Tooltip("Minimum velocity magnitude to show trail. Prevents trail on slow/stationary projectiles.")]
        public float minVelocityThreshold = 1f;

        /// <summary>
        /// Creates a default trail configuration with sensible values.
        /// </summary>
        public static V2TrailConfig Default => new V2TrailConfig
        {
            enabled = true,
            length = 1.5f,
            width = 0f,
            widthMultiplier = 0.8f,
            growTime = 0.1f,
            useProjectileColor = true,
            color = Color.white,
            glowIntensity = 1.5f,
            falloffPower = 2f,
            softness = 0.3f,
            sortingOrderOffset = -1,
            minVelocityThreshold = 1f
        };
    }
}
