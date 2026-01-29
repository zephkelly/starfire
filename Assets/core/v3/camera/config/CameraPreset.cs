using Starfire.Core.V3.Cam.Effects;
using UnityEngine;

namespace Starfire.Core.V3.Cam.Config
{
    [CreateAssetMenu(fileName = "CameraPreset", menuName = "Starfire/Camera/V3/Camera Preset")]
    public class CameraPreset : ScriptableObject
    {
        [Header("Aim Look-Ahead")]
        [Tooltip("Minimum look-ahead when cursor is near center")]
        public float minLookAhead = 0f;

        [Tooltip("Maximum look-ahead when cursor is at outer limit")]
        public float maxLookAhead = 5f;

        [Tooltip("Screen distance from center (0-1 of half-screen) where min look-ahead ends")]
        [Range(0f, 1f)]
        public float innerLimit = 0.1f;

        [Tooltip("Screen distance from center (0-1 of half-screen) where max look-ahead is reached")]
        [Range(0f, 1f)]
        public float outerLimit = 0.8f;

        [Tooltip("Curve controlling look-ahead ramp (X: 0-1 normalized distance, Y: 0-1 lerp factor)")]
        public AnimationCurve lookAheadCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Smoothing time for look-ahead transitions (0 = instant)")]
        public float lookAheadSmoothing = 0.15f;

        [Header("Zoom")]
        public float orthographicSize = 10f;
        public float minZoom = 5f;
        public float maxZoom = 20f;

        [Tooltip("Base scroll sensitivity")]
        public float scrollSensitivity = 2f;

        [Tooltip("Curve controlling zoom rate based on current zoom level (X: 0-1 normalized zoom, Y: multiplier)")]
        public AnimationCurve zoomRateCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 2f);

        [Tooltip("Smoothing time for zoom transitions (0 = instant)")]
        public float zoomSmoothing = 0.1f;

        [Header("Screen Shake")]
        [Tooltip("Configuration for screen shake behavior.")]
        public V3ScreenShakeConfig screenShakeConfig = new V3ScreenShakeConfig();
    }
}
