using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for screen shake effects triggered when firing a weapon.
    /// Embedded in OffensiveWeaponModuleConfig for per-weapon configuration.
    /// </summary>
    [Serializable]
    public class V2FireShakeConfig
    {
        /// <summary>
        /// Reference ortho size for distance scaling. At this size, maxEffectDistance is used directly.
        /// </summary>
        public const float ReferenceOrthoSize = 10f;

        [Header("Trauma (Perlin Noise Shake)")]
        [Tooltip("Base trauma amount to add before distance attenuation (0-1).")]
        [Range(0f, 1f)]
        public float traumaAmount = 0.1f;

        [Header("Punch (Recoil Impulse)")]
        [Tooltip("Enable directional punch effect simulating recoil.")]
        public bool enablePunch = true;

        [Tooltip("Base punch force before distance attenuation.")]
        public float punchForce = 0.2f;

        [Tooltip("Duration of the punch effect in seconds.")]
        public float punchDuration = 0.1f;

        [Header("Distance Attenuation")]
        [Tooltip("Maximum distance at which effects are felt (in world units at reference ortho size 10).")]
        public float maxEffectDistance = 30f;

        [Tooltip("Attenuation curve (X: 0-1 normalized distance, Y: 0-1 effect multiplier). Default eases from full to zero.")]
        public AnimationCurve attenuationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Tooltip("Whether to scale max distance with camera ortho size. Larger zoom = wider effect range.")]
        public bool scaleWithOrthoSize = true;

        /// <summary>
        /// Default fire shake configuration with light recoil values.
        /// </summary>
        public static V2FireShakeConfig Default => new V2FireShakeConfig
        {
            traumaAmount = 0.1f,
            enablePunch = true,
            punchForce = 0.2f,
            punchDuration = 0.1f,
            maxEffectDistance = 30f,
            attenuationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f),
            scaleWithOrthoSize = true
        };
    }
}
