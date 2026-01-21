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

        /// <summary>
        /// Get the wake intensity multiplier for a given warp intensity.
        /// </summary>
        public float GetIntensityMultiplier(float warpIntensity)
        {
            return intensityCurve.Evaluate(warpIntensity);
        }
    }
}
