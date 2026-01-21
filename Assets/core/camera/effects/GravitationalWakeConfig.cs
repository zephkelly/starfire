using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// Configuration asset for the gravitational wake distortion effect.
    /// Assign this to the GravitationalWakeFeature in the renderer.
    /// </summary>
    [CreateAssetMenu(fileName = "GravitationalWakeConfig", menuName = "Starfire/Effects/Gravitational Wake Config")]
    public class GravitationalWakeConfig : ScriptableObject
    {
        [Header("Bubble Zone")]
        [Tooltip("Radius of undistorted zone around ship center (screen space 0-0.5)")]
        [Range(0f, 0.3f)] public float bubbleRadius = 0.08f;

        [Tooltip("Width of the distortion ring around the bubble")]
        [Range(0.05f, 0.3f)] public float ringWidth = 0.15f;

        [Header("Wake Trail")]
        [Tooltip("How far the wake trail extends behind the ship")]
        [Range(0.1f, 1f)] public float trailLength = 0.5f;

        [Tooltip("How quickly wake fades with distance from ship")]
        [Range(0.5f, 3f)] public float trailFalloff = 1.5f;

        [Tooltip("How strongly wake favors trailing direction (1 = only behind, 0 = symmetric)")]
        [Range(0f, 1f)] public float directionalBias = 0.7f;

        [Header("Distortion Strength")]
        [Tooltip("Maximum UV distortion strength")]
        [Range(0f, 0.15f)] public float distortionStrength = 0.03f;

        [Tooltip("Curve mapping warp intensity (0-1) to wake effect strength")]
        public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Chromatic Aberration")]
        [Tooltip("Enable subtle chromatic aberration in wake zone")]
        public bool chromaEnabled = true;

        [Tooltip("Chromatic aberration strength in wake")]
        [Range(0f, 0.02f)] public float chromaStrength = 0.003f;

        [Header("Activation")]
        [Tooltip("Minimum warp intensity to activate the effect")]
        [Range(0f, 0.5f)] public float activationThreshold = 0.01f;

        [Header("Animation - Breathing")]
        [Tooltip("Speed of bubble breathing animation")]
        [Range(0.5f, 4f)] public float pulseSpeed = 1.5f;

        [Tooltip("How much bubble radius breathes (0 = none, 0.3 = 30%)")]
        [Range(0f, 0.3f)] public float pulseAmount = 0.15f;

        [Header("Animation - Ripples")]
        [Tooltip("Number of ripple waves emanating from bubble")]
        [Range(1f, 5f)] public float rippleCount = 2f;

        [Tooltip("Speed of ripples flowing outward")]
        [Range(0.1f, 1f)] public float rippleSpeed = 0.3f;

        [Tooltip("Strength of ripple distortion")]
        [Range(0f, 0.5f)] public float rippleStrength = 0.25f;

        [Header("Animation - Wobble")]
        [Tooltip("Scale of noise pattern for wobble effect")]
        [Range(5f, 30f)] public float noiseScale = 12f;

        [Tooltip("Speed of noise animation")]
        [Range(0.1f, 2f)] public float noiseSpeed = 0.5f;

        [Tooltip("Strength of noise-based wobble")]
        [Range(0f, 0.5f)] public float noiseStrength = 0.2f;

        [Header("Animation - Bow Wave")]
        [Tooltip("Bow wave strength relative to trailing wake (piercing effect at front)")]
        [Range(0f, 0.5f)] public float bowWaveStrength = 0.35f;

        /// <summary>
        /// Get the wake intensity multiplier for a given warp intensity.
        /// </summary>
        public float GetIntensityMultiplier(float warpIntensity)
        {
            return intensityCurve.Evaluate(warpIntensity);
        }
    }
}
