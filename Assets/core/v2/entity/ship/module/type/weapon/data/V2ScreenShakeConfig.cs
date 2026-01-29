using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for screen shake effects triggered by projectile impacts.
    /// Embedded in V2ImpactConfig for per-projectile configuration.
    /// </summary>
    [Serializable]
    public class V2ScreenShakeConfig
    {
        /// <summary>
        /// Reference ortho size for distance scaling. At this size, maxEffectDistance is used directly.
        /// </summary>
        public const float ReferenceOrthoSize = 10f;

        [Header("Trauma (Perlin Noise Shake)")]
        [Tooltip("Base trauma amount to add before distance attenuation (0-1).")]
        [Range(0f, 1f)]
        public float traumaAmount = 0.3f;

        [Header("Punch (Directional Impulse)")]
        [Tooltip("Enable directional punch effect in addition to trauma shake.")]
        public bool enablePunch = false;

        [Tooltip("Base punch force before distance attenuation.")]
        public float punchForce = 0.5f;

        [Tooltip("Duration of the punch effect in seconds.")]
        public float punchDuration = 0.15f;

        [Tooltip("Use hit normal for punch direction. If false, uses projectile direction.")]
        public bool useHitNormal = true;

        [Header("Distance Attenuation")]
        [Tooltip("Maximum distance at which effects are felt (in world units at reference ortho size 10).")]
        public float maxEffectDistance = 50f;

        [Tooltip("Attenuation curve (X: 0-1 normalized distance, Y: 0-1 effect multiplier). Default eases from full to zero.")]
        public AnimationCurve attenuationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Tooltip("Whether to scale max distance with camera ortho size. Larger zoom = wider effect range.")]
        public bool scaleWithOrthoSize = true;

        /// <summary>
        /// Default screen shake configuration with moderate values.
        /// </summary>
        public static V2ScreenShakeConfig Default => new V2ScreenShakeConfig
        {
            traumaAmount = 0.3f,
            enablePunch = false,
            punchForce = 0.5f,
            punchDuration = 0.15f,
            useHitNormal = true,
            maxEffectDistance = 50f,
            attenuationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f),
            scaleWithOrthoSize = true
        };
    }
}
