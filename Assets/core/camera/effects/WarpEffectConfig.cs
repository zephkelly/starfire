using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    [CreateAssetMenu(fileName = "WarpEffectConfig", menuName = "Starfire/Effects/Warp Config")]
    public class WarpEffectConfig : ScriptableObject
    {
        [Header("Star Streaking")]
        [Tooltip("Maximum stretch multiplier at full warp intensity")]
        [Range(1f, 50f)] public float maxStretchMultiplier = 20f;

        [Tooltip("Curve mapping warp intensity (0-1) to stretch amount")]
        public AnimationCurve stretchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Brightness multiplier at full warp (< 1 dims stars, > 1 brightens)")]
        [Range(0.1f, 3f)] public float warpBrightnessMultiplier = 1.0f;

        [Header("Streak Shape")]
        [Tooltip("How thin the streak becomes at full warp (0.1 = very thin, 1.0 = no thinning)")]
        [Range(0.05f, 1f)] public float streakWidthAtFullWarp = 0.2f;

        [Tooltip("Edge sharpness at low warp (higher = softer edges)")]
        [Range(0.01f, 0.5f)] public float edgeSoftnessMin = 0.3f;

        [Tooltip("Edge sharpness at full warp (lower = crisper edges)")]
        [Range(0.01f, 0.5f)] public float edgeSoftnessMax = 0.05f;

        [Tooltip("Leading edge length as ratio of streak length (smaller = sharper front)")]
        [Range(0.05f, 0.5f)] public float leadingEdgeRatio = 0.15f;

        [Tooltip("Where trailing fade begins as ratio of trail length (smaller = earlier fade)")]
        [Range(0.1f, 0.9f)] public float trailingFadeStart = 0.3f;

        [Tooltip("Blend transition range - how quickly stars transition to streaks (0-1 intensity range)")]
        [Range(0.05f, 0.5f)] public float blendTransitionRange = 0.15f;

        [Tooltip("Tail taper curve power (< 1 = slower taper/fatter tail, > 1 = faster taper/sharper point)")]
        [Range(0.3f, 2f)] public float tailTaperPower = 0.7f;

        [Header("Streak Wobble")]
        [Tooltip("Intensity of streak wobble (0 = disabled, higher = more wavy). Only affects the tail, not the head.")]
        [Range(0f, 0.5f)] public float wobbleIntensity = 0f;

        [Tooltip("Frequency of wobble waves along streak length")]
        [Range(0.5f, 15f)] public float wobbleFrequency = 5f;

        [Tooltip("Animation speed of wobble")]
        [Range(0f, 5f)] public float wobbleSpeed = 2f;

        [Header("Parallax Depth Scaling")]
        [Tooltip("Minimum warp effect for distant layers (0 = no effect, 1 = full effect)")]
        [Range(0f, 1f)] public float distantLayerMinEffect = 0.3f;

        [Tooltip("Parallax multiplier for depth scaling (higher = more difference between layers)")]
        [Range(1f, 20f)] public float parallaxDepthMultiplier = 10f;

        [Header("Lens Distortion")]
        [Tooltip("Maximum lens distortion at full warp (positive = barrel, negative = pincushion)")]
        [Range(-0.5f, 0.5f)] public float maxLensDistortion = 0.15f;

        [Tooltip("Curve mapping warp intensity to lens distortion")]
        public AnimationCurve lensDistortionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Zoom")]
        [Tooltip("Zoom multiplier at full warp (>1 zooms out, <1 zooms in)")]
        [Range(0.5f, 2f)] public float maxZoomMultiplier = 1.2f;

        [Tooltip("Curve mapping warp intensity to zoom")]
        public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Nebula Effects")]
        [Tooltip("Max nebula expansion at full warp (0.1 = 10% expansion, 0.5 = 50% expansion)")]
        [Range(0f, 1f)] public float nebulaStretchRatio = 0.3f;

        [Tooltip("How much nebulae fade at full warp")]
        [Range(0f, 1f)] public float nebulaFadeAtFullWarp = 0.3f;

        [Header("Speed-Based Settings")]
        [Tooltip("Minimum speed to start showing warp effect")]
        [Min(0f)] public float minSpeedForEffect = 5f;

        [Tooltip("Speed at which warp effect reaches full intensity")]
        [Min(0f)] public float maxSpeedForEffect = 50f;

        [Tooltip("Curve mapping normalized speed (0-1) to warp intensity")]
        public AnimationCurve speedToIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Smoothing time for intensity changes")]
        [Range(0.01f, 1f)] public float intensitySmoothing = 0.15f;

        [Header("Manual Warp Timing")]
        [Tooltip("Default duration for warping in (manual mode)")]
        [Min(0.1f)] public float defaultWarpInDuration = 1.5f;

        [Tooltip("Default duration for warping out (manual mode)")]
        [Min(0.1f)] public float defaultWarpOutDuration = 1.0f;

        [Tooltip("Easing curve for manual warp transitions")]
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Gravitational Wake Effect")]
        [Tooltip("Radius of undistorted zone around ship center (screen space 0-0.5)")]
        [Range(0f, 0.3f)] public float wakeBubbleRadius = 0.08f;

        [Tooltip("Width of the distortion ring around the bubble")]
        [Range(0.05f, 0.3f)] public float wakeRingWidth = 0.15f;

        [Tooltip("How far the wake trail extends behind the ship")]
        [Range(0.1f, 1f)] public float wakeTrailLength = 0.5f;

        [Tooltip("Maximum UV distortion strength")]
        [Range(0f, 0.1f)] public float wakeDistortionStrength = 0.03f;

        [Tooltip("How quickly wake fades with distance from ship")]
        [Range(0.5f, 3f)] public float wakeTrailFalloff = 1.5f;

        [Tooltip("How strongly wake favors trailing direction (1 = only behind, 0 = symmetric)")]
        [Range(0f, 1f)] public float wakeDirectionalBias = 0.7f;

        [Tooltip("Curve mapping warp intensity to wake effect strength")]
        public AnimationCurve wakeIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Enable subtle chromatic aberration in wake zone")]
        public bool wakeChromaEnabled = true;

        [Tooltip("Chromatic aberration strength in wake")]
        [Range(0f, 0.01f)] public float wakeChromaStrength = 0.002f;

        [Header("Star Density Reduction")]
        [Tooltip("How much to reduce star spawn chance at full warp (0 = no reduction, 1 = all stars gone)")]
        [Range(0f, 1f)] public float warpStarFadeMax = 0.7f;

        [Tooltip("Curve mapping warp intensity (0-1) to star density reduction factor (0-1)")]
        public AnimationCurve warpStarFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("How much parallax depth affects star fade. 0 = background fades more, 1 = foreground fades more")]
        [Range(0f, 1f)] public float warpStarFadeNearBias = 0.5f;

        [Tooltip("Minimum fade factor for the least-affected depth layer (0 = can fully preserve, 1 = always fade equally)")]
        [Range(0f, 1f)] public float warpStarFadeMinDepth = 0.3f;

        /// <summary>
        /// Evaluate the stretch multiplier for a given warp intensity.
        /// </summary>
        public float GetStretchMultiplier(float intensity)
        {
            float curvedIntensity = stretchCurve.Evaluate(intensity);
            return Mathf.Lerp(1f, maxStretchMultiplier, curvedIntensity);
        }

        /// <summary>
        /// Evaluate the lens distortion for a given warp intensity.
        /// </summary>
        public float GetLensDistortion(float intensity)
        {
            return lensDistortionCurve.Evaluate(intensity) * maxLensDistortion;
        }

        /// <summary>
        /// Evaluate the zoom multiplier for a given warp intensity.
        /// </summary>
        public float GetZoomMultiplier(float intensity)
        {
            float curvedIntensity = zoomCurve.Evaluate(intensity);
            return Mathf.Lerp(1f, maxZoomMultiplier, curvedIntensity);
        }

        /// <summary>
        /// Evaluate the brightness multiplier for a given warp intensity.
        /// </summary>
        public float GetBrightnessMultiplier(float intensity)
        {
            return Mathf.Lerp(1f, warpBrightnessMultiplier, intensity);
        }

        /// <summary>
        /// Evaluate the nebula stretch multiplier for a given warp intensity.
        /// </summary>
        public float GetNebulaStretchMultiplier(float intensity)
        {
            return 1f + nebulaStretchRatio * intensity;
        }

        /// <summary>
        /// Evaluate the nebula fade for a given warp intensity.
        /// </summary>
        public float GetNebulaFade(float intensity)
        {
            return intensity * nebulaFadeAtFullWarp;
        }

        /// <summary>
        /// Evaluate the wake effect intensity for a given warp intensity.
        /// </summary>
        public float GetWakeIntensity(float intensity)
        {
            return wakeIntensityCurve.Evaluate(intensity);
        }

        /// <summary>
        /// Evaluate the star density reduction for a given warp intensity.
        /// Returns 0 (no fade) to warpStarFadeMax (maximum fade).
        /// </summary>
        public float GetWarpStarFade(float intensity)
        {
            return warpStarFadeCurve.Evaluate(intensity) * warpStarFadeMax;
        }
    }
}
