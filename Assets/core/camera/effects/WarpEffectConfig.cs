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

        [Header("Chromatic Aberration")]
        [Tooltip("Maximum chromatic aberration intensity at full warp")]
        [Range(0f, 1f)] public float maxChromaticIntensity = 0.7f;

        [Tooltip("Curve mapping warp intensity to chromatic aberration")]
        public AnimationCurve chromaticCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
        [Tooltip("How much nebulae stretch relative to stars (0 = no stretch, 1 = same as stars)")]
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

        /// <summary>
        /// Evaluate the stretch multiplier for a given warp intensity.
        /// </summary>
        public float GetStretchMultiplier(float intensity)
        {
            float curvedIntensity = stretchCurve.Evaluate(intensity);
            return Mathf.Lerp(1f, maxStretchMultiplier, curvedIntensity);
        }

        /// <summary>
        /// Evaluate the chromatic aberration for a given warp intensity.
        /// </summary>
        public float GetChromaticAberration(float intensity)
        {
            return chromaticCurve.Evaluate(intensity) * maxChromaticIntensity;
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
            float starStretch = GetStretchMultiplier(intensity);
            return Mathf.Lerp(1f, 1f + (starStretch - 1f) * nebulaStretchRatio, intensity);
        }

        /// <summary>
        /// Evaluate the nebula fade for a given warp intensity.
        /// </summary>
        public float GetNebulaFade(float intensity)
        {
            return intensity * nebulaFadeAtFullWarp;
        }
    }
}
