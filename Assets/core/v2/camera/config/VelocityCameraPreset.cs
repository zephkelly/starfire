using System;
using UnityEngine;

namespace Starfire.Core.V2.Cam.Config
{
    [CreateAssetMenu(fileName = "VelocityCameraPreset", menuName = "Starfire/Camera/V2/Velocity Camera Preset")]
    public class VelocityCameraPreset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string presetId = "default";
        [SerializeField] private string displayName = "Default Velocity Preset";

        [Header("Velocity Tracking")]
        [Tooltip("Speed below which traditional smooth follow is used")]
        [SerializeField] private float velocityTrackingThreshold = 5f;
        [Tooltip("Speed at which velocity tracking reaches full effect (100%)")]
        [SerializeField] private float velocityTrackingFullSpeed = 15f;
        [Tooltip("Time to correct position error when velocity tracking (lower = snappier)")]
        [SerializeField] private float positionCorrectionTime = 0.1f;
        [Tooltip("Maximum position error allowed before emergency correction")]
        [SerializeField] private float maxPositionError = 2f;
        [Tooltip("Blend curve for tracking factor (X=normalized speed, Y=tracking 0-1)")]
        [SerializeField] private AnimationCurve velocityTrackingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Low Speed Following")]
        [Tooltip("SmoothDamp time at low speed")]
        [SerializeField] private float lowSpeedDampening = 0.15f;

        [Header("Focus Look-Ahead")]
        [Tooltip("Maximum focus look-ahead distance toward cursor/aim")]
        [SerializeField] private float focusLookAheadDistance = 3f;
        [Tooltip("Smoothing time for focus offset changes")]
        [SerializeField] private float focusSmoothing = 0.2f;
        [Tooltip("Scale focus offset with zoom level")]
        [SerializeField] private bool scaleFocusWithZoom = true;
        [Tooltip("How focus influence scales with speed (X=normalized speed, Y=influence 0-1)")]
        [SerializeField] private AnimationCurve focusSpeedInfluence = AnimationCurve.Linear(0, 1, 1, 0.3f);

        [Header("Zoom")]
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 15f;
        [SerializeField] private float defaultZoom = 10f;
        [SerializeField] private float zoomSpeed = 2f;

        [Header("Speed-Based Zoom")]
        [SerializeField] private bool enableSpeedZoom = true;
        [SerializeField] private float speedZoomMinSpeed = 0f;
        [SerializeField] private float speedZoomMaxSpeed = 20f;
        [SerializeField] private AnimationCurve speedZoomCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [Range(0f, 1f)]
        [SerializeField] private float speedZoomInfluence = 0.5f;

        [Header("Scroll Zoom")]
        [SerializeField] private float scrollZoomSensitivity = 1f;
        [SerializeField] private float scrollZoomSmoothing = 0.15f;

        [Header("Zoom Overshoot")]
        [SerializeField] private bool enableZoomOvershoot = true;
        [SerializeField] private float maxZoomOvershoot = 2f;
        [SerializeField] private float overshootRecoverySpeed = 3f;

        [Header("Multi-Target")]
        [SerializeField] private float multiTargetPadding = 3f;
        [SerializeField] private float multiTargetMinZoom = 5f;
        [SerializeField] private float multiTargetMaxZoom = 20f;

        [Header("Screen Bounds")]
        [SerializeField] private bool enableScreenBounds = true;
        [Range(0f, 0.5f)]
        [SerializeField] private float screenBoundsMargin = 0.15f;

        [Header("Effects")]
        [SerializeField] private float shakeMultiplier = 1f;
        [SerializeField] private float maxShakeOffset = 0.5f;
        [SerializeField] private float maxShakeRotation = 3f;
        [SerializeField] private float shakeTraumaDecay = 1.5f;
        [SerializeField] private float shakeFrequency = 15f;

        // Identity
        public string PresetId => presetId;
        public string DisplayName => displayName;

        // Velocity Tracking
        public float VelocityTrackingThreshold => velocityTrackingThreshold;
        public float VelocityTrackingFullSpeed => velocityTrackingFullSpeed;
        public float PositionCorrectionTime => positionCorrectionTime;
        public float MaxPositionError => maxPositionError;
        public AnimationCurve VelocityTrackingCurve => velocityTrackingCurve;

        // Low Speed Following
        public float LowSpeedDampening => lowSpeedDampening;

        // Focus Look-Ahead
        public float FocusLookAheadDistance => focusLookAheadDistance;
        public float FocusSmoothing => focusSmoothing;
        public bool ScaleFocusWithZoom => scaleFocusWithZoom;
        public AnimationCurve FocusSpeedInfluence => focusSpeedInfluence;

        // Zoom
        public float MinZoom => minZoom;
        public float MaxZoom => maxZoom;
        public float DefaultZoom => defaultZoom;
        public float ZoomSpeed => zoomSpeed;

        // Speed-Based Zoom
        public bool EnableSpeedZoom => enableSpeedZoom;
        public float SpeedZoomMinSpeed => speedZoomMinSpeed;
        public float SpeedZoomMaxSpeed => speedZoomMaxSpeed;
        public AnimationCurve SpeedZoomCurve => speedZoomCurve;
        public float SpeedZoomInfluence => speedZoomInfluence;

        // Scroll Zoom
        public float ScrollZoomSensitivity => scrollZoomSensitivity;
        public float ScrollZoomSmoothing => scrollZoomSmoothing;

        // Zoom Overshoot
        public bool EnableZoomOvershoot => enableZoomOvershoot;
        public float MaxZoomOvershoot => maxZoomOvershoot;
        public float OvershootRecoverySpeed => overshootRecoverySpeed;

        // Multi-Target
        public float MultiTargetPadding => multiTargetPadding;
        public float MultiTargetMinZoom => multiTargetMinZoom;
        public float MultiTargetMaxZoom => multiTargetMaxZoom;

        // Screen Bounds
        public bool EnableScreenBounds => enableScreenBounds;
        public float ScreenBoundsMargin => screenBoundsMargin;

        // Effects
        public float ShakeMultiplier => shakeMultiplier;
        public float MaxShakeOffset => maxShakeOffset;
        public float MaxShakeRotation => maxShakeRotation;
        public float ShakeTraumaDecay => shakeTraumaDecay;
        public float ShakeFrequency => shakeFrequency;

        public VelocityCameraPresetInstance CreateInstance()
        {
            return new VelocityCameraPresetInstance(this);
        }

#if UNITY_EDITOR
        public static event Action<VelocityCameraPreset> OnPresetModified;

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                OnPresetModified?.Invoke(this);
            }
        }
#endif
    }
}
