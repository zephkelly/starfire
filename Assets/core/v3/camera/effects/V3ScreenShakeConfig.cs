using System;
using UnityEngine;

namespace Starfire.Core.V3.Cam.Effects
{
    /// <summary>
    /// Global configuration for screen shake behavior.
    /// Embedded in CameraPreset for scene-wide settings.
    /// </summary>
    [Serializable]
    public class V3ScreenShakeConfig
    {
        [Header("Shake Behavior")]
        [Tooltip("Global multiplier for all trauma additions.")]
        public float shakeMultiplier = 1f;

        [Tooltip("Maximum positional offset during shake (world units).")]
        public float maxOffset = 0.5f;

        [Tooltip("Maximum rotational offset during shake (degrees).")]
        public float maxRotation = 3f;

        [Tooltip("Rate at which trauma decays per second.")]
        public float traumaDecay = 1.5f;

        [Tooltip("Perlin noise frequency for shake variation.")]
        public float frequency = 25f;

        /// <summary>
        /// Default screen shake config with sensible values.
        /// </summary>
        public static V3ScreenShakeConfig Default => new V3ScreenShakeConfig
        {
            shakeMultiplier = 1f,
            maxOffset = 0.5f,
            maxRotation = 3f,
            traumaDecay = 1.5f,
            frequency = 25f
        };
    }
}
