using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    /// <summary>
    /// Configuration asset for the spacetime tearing effect during warp.
    /// Controls how starfield layers separate and fracture lines appear.
    /// </summary>
    [CreateAssetMenu(fileName = "TearingEffectConfig", menuName = "Starfire/Effects/Tearing Effect Config")]
    public class TearingEffectConfig : ScriptableObject
    {
        [Header("Layer Separation")]
        [Tooltip("Maximum UV offset at full warp for closest layers (creates visible gaps)")]
        [Range(0f, 0.15f)] public float maxTearOffset = 0.03f;

        [Tooltip("How much parallax depth affects tear amount (higher = more depth-based variation)")]
        [Range(1f, 20f)] public float tearDepthMultiplier = 8f;

        [Tooltip("Curve mapping warp intensity (0-1) to tear effect strength")]
        public AnimationCurve tearIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Fracture Lines")]
        [Tooltip("Enable glowing fracture/crack lines at layer boundaries")]
        public bool fractureEnabled = true;

        [Tooltip("Width of fracture lines (thinner = sharper cracks)")]
        [Range(0.001f, 0.015f)] public float fractureLineWidth = 0.004f;

        [Tooltip("Scale of fracture noise pattern (higher = more detailed cracks)")]
        [Range(5f, 40f)] public float fractureNoiseScale = 15f;

        [Tooltip("Color of fracture lines (HDR for bloom support)")]
        [ColorUsage(true, true)]
        public Color fractureColor = new Color(0.5f, 0.8f, 1f, 1f);

        [Tooltip("Glow intensity of fracture lines (values above 1 trigger bloom)")]
        [Range(0f, 5f)] public float fractureGlowIntensity = 1.5f;

        [Header("Wake Propagation")]
        [Tooltip("How far tearing extends into the wake trail behind the ship")]
        [Range(0.1f, 1.5f)] public float tearWakeLength = 0.6f;

        [Tooltip("How quickly tearing fades with distance in the wake")]
        [Range(0.5f, 3f)] public float tearWakeFalloff = 1.5f;

        [Header("Animation")]
        [Tooltip("Speed of fracture line flickering")]
        [Range(0f, 15f)] public float tearFlickerSpeed = 4f;

        [Tooltip("Intensity variation of flickering (0 = steady, 1 = full flicker)")]
        [Range(0f, 0.6f)] public float tearFlickerAmount = 0.25f;

        [Header("Activation")]
        [Tooltip("Minimum warp intensity to activate tearing effect")]
        [Range(0f, 0.3f)] public float activationThreshold = 0.05f;

        /// <summary>
        /// Get the tear intensity multiplier for a given warp intensity.
        /// </summary>
        public float GetTearIntensity(float warpIntensity)
        {
            if (warpIntensity < activationThreshold) return 0f;
            return tearIntensityCurve.Evaluate(warpIntensity);
        }
    }
}
