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
        [Range(0f, 0.5f)] public float bubbleRadius = 0.15f;

        [Tooltip("Width of the distortion ring around the bubble")]
        [Range(0.05f, 0.5f)] public float ringWidth = 0.2f;

        [Tooltip("Camera ortho size where bubble radius values are calibrated. Effect scales to maintain world-space size.")]
        [Range(1f, 50f)] public float referenceOrthoSize = 10f;

        [Header("Ellipse Shape")]
        [Tooltip("Ratio of minor to major axis (1 = circle, 0.3 = narrow ellipse aligned with movement)")]
        [Range(0.2f, 1f)] public float ellipseRatio = 1f;

        [Tooltip("How pointed the front edge becomes (0 = uniform ellipse, 1 = sharp needle)")]
        [Range(0f, 1f)] public float needleSharpness = 0f;

        [Header("Wake Trail")]
        [Tooltip("How far the wake trail extends behind the ship")]
        [Range(0.1f, 1f)] public float trailLength = 0.5f;

        [Tooltip("How quickly wake fades with distance from ship")]
        [Range(0.5f, 3f)] public float trailFalloff = 1.5f;

        [Tooltip("How strongly wake favors trailing direction (1 = only behind, 0 = symmetric)")]
        [Range(0f, 1f)] public float directionalBias = 0.7f;

        [Header("Distortion Strength")]
        [Tooltip("Maximum UV distortion strength")]
        [Range(0f, 0.3f)] public float distortionStrength = 0.03f;

        [Tooltip("Curve mapping warp intensity (0-1) to wake effect strength")]
        public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Bubble Interior")]
        [Tooltip("Allow distortion inside the bubble zone (0 = protected interior, 1 = full distortion everywhere)")]
        [Range(0f, 1f)] public float bubbleInteriorDistortion = 0f;

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

        [Header("Edge Distortion (Event Horizon)")]
        [Tooltip("Extreme distortion strength at bubble edge")]
        [Range(0f, 1f)] public float edgeDistortionStrength = 0.5f;

        [Tooltip("Sharpness of edge distortion falloff (higher = sharper edge)")]
        [Range(1f, 10f)] public float edgeSharpness = 4f;

        [Header("Wake Zone (Full Turbulence)")]
        [Tooltip("Angular spread of wake behind ship (degrees from center)")]
        [Range(15f, 90f)] public float wakeAngle = 45f;

        [Tooltip("Turbulence strength in wake zone")]
        [Range(0f, 1f)] public float wakeTurbulence = 0.6f;

        [Tooltip("Scale of turbulence vortices in wake")]
        [Range(3f, 20f)] public float wakeTurbulenceScale = 8f;

        [Tooltip("Speed of turbulence animation")]
        [Range(0.1f, 3f)] public float wakeTurbulenceSpeed = 1f;

        [Tooltip("How much wake turbulence increases with distance")]
        [Range(0f, 2f)] public float wakeSpread = 1f;

        [Header("Energy Glow")]
        [Tooltip("Enable glowing energy rim at bubble edge")]
        public bool energyGlowEnabled = true;

        [Tooltip("Primary glow color")]
        public Color energyGlowColor = new Color(0.3f, 0.7f, 1f, 1f);

        [Tooltip("Glow intensity (values above 1 can trigger bloom)")]
        [Range(0f, 3f)] public float energyGlowIntensity = 1f;

        [Tooltip("Glow width relative to ring width")]
        [Range(0.1f, 1f)] public float energyGlowWidth = 0.5f;

        [Tooltip("Flow speed around the bubble (rotational animation)")]
        [Range(0f, 5f)] public float energyFlowSpeed = 1f;

        [Tooltip("Number of energy bands flowing around bubble")]
        [Range(1f, 10f)] public float energyFlowBands = 3f;

        [Header("Front Deflector")]
        [Tooltip("Enable bright glow at the front piercing point")]
        public bool deflectorGlowEnabled = true;

        [Tooltip("Deflector glow color (HDR for bloom)")]
        [ColorUsage(true, true)]
        public Color deflectorGlowColor = new Color(0.5f, 0.8f, 1f, 1f);

        [Tooltip("Deflector glow intensity (values above 1 trigger bloom)")]
        [Range(0f, 5f)] public float deflectorGlowIntensity = 2f;

        [Tooltip("Size of the deflector glow point")]
        [Range(0.01f, 0.2f)] public float deflectorGlowSize = 0.08f;

        [Tooltip("Pulse speed for deflector glow animation")]
        [Range(0.5f, 5f)] public float deflectorPulseSpeed = 3f;

        [Header("Turbulent Boundary (Fluid Aurora)")]
        [Tooltip("Enable turbulent fluid-like effect along bubble boundary")]
        public bool turbBoundaryEnabled = true;

        [Tooltip("Overall intensity of turbulent boundary effect")]
        [Range(0f, 2f)] public float turbBoundaryIntensity = 1f;

        [Tooltip("Scale of turbulence noise pattern")]
        [Range(5f, 30f)] public float turbBoundaryScale = 15f;

        [Tooltip("Animation speed of turbulence")]
        [Range(0.1f, 3f)] public float turbBoundarySpeed = 1f;

        [Tooltip("Number of wave oscillations around bubble")]
        [Range(2f, 12f)] public float turbBoundaryWaveCount = 6f;

        [Tooltip("Amplitude of wave displacement")]
        [Range(0f, 1f)] public float turbBoundaryWaveAmplitude = 0.5f;

        [Tooltip("How much turbulence affects color hue")]
        [Range(0f, 1f)] public float turbBoundaryColorShift = 0.3f;

        [Tooltip("How much effect disperses/spreads into wake")]
        [Range(0f, 3f)] public float turbBoundaryDispersion = 1.5f;

        /// <summary>
        /// Get the wake intensity multiplier for a given warp intensity.
        /// </summary>
        public float GetIntensityMultiplier(float warpIntensity)
        {
            return intensityCurve.Evaluate(warpIntensity);
        }
    }
}
