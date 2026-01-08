using UnityEngine;

namespace Starfire.Core.Cam.Config
{
    [CreateAssetMenu(fileName = "CameraPreset", menuName = "Starfire/Camera/Camera Preset")]
    public class CameraPreset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string presetId = "default";
        [SerializeField] private string displayName = "Default Preset";

        [Header("Following")]
        [Tooltip("Follow dampening at low speed")]
        [SerializeField] private float followDampeningMax = 0.1f;
        [Tooltip("Follow dampening at high speed (faster following)")]
        [SerializeField] private float followDampeningMin = 0.02f;

        [Header("Velocity Look-Ahead")]
        [Tooltip("Base look-ahead distance at reference speed")]
        [SerializeField] private float velocityLookAheadDistance = 2f;
        [SerializeField] private float velocityLookAheadSmoothing = 0.15f;
        [Tooltip("Scale look-ahead with zoom to maintain screen-space offset")]
        [SerializeField] private bool velocityLookAheadScaleWithZoom = true;
        [Tooltip("Predictive look-ahead in seconds (where ship will be)")]
        [SerializeField] private float velocityLookAheadPredictionTime = 0.3f;

        [Header("Focus Look-Ahead")]
        [Tooltip("Camera leads toward aim/cursor direction")]
        [SerializeField] private float focusLookAheadDistance = 1.5f;
        [SerializeField] private float focusLookAheadSmoothing = 0.3f;
        [Tooltip("How focus look-ahead scales with speed (X=normalized speed 0-1, Y=effect multiplier 0-1)")]
        [SerializeField] private AnimationCurve focusLookAheadSpeedCurve = AnimationCurve.Constant(0, 1, 1);

        [Header("Look-Ahead Limits")]
        [Tooltip("Maximum combined look-ahead distance (0 = unlimited). Prevents camera from offsetting too far when velocity and focus oppose each other.")]
        [Min(0)]
        [SerializeField] private float maxLookAheadDistance = 0f;

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
        [Tooltip("How much speed affects zoom relative to user zoom (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float speedZoomInfluence = 0.5f;

        [Header("Scroll Zoom")]
        [Tooltip("Zoom change per scroll step")]
        [SerializeField] private float scrollZoomSensitivity = 1f;
        [SerializeField] private float scrollZoomSmoothing = 0.15f;

        [Header("Zoom Overshoot")]
        [Tooltip("Allow momentary zoom beyond limits when speed pushes hard")]
        [SerializeField] private bool enableZoomOvershoot = true;
        [Tooltip("Max extra zoom allowed beyond limits")]
        [SerializeField] private float maxZoomOvershoot = 2f;
        [Tooltip("How quickly overshoot snaps back")]
        [SerializeField] private float overshootRecoverySpeed = 3f;

        [Header("Multi-Target")]
        [SerializeField] private float multiTargetPadding = 3f;
        [SerializeField] private float multiTargetMinZoom = 5f;
        [SerializeField] private float multiTargetMaxZoom = 20f;

        [Header("Screen Bounds")]
        [Tooltip("Keep target within screen bounds")]
        [SerializeField] private bool enableScreenBounds = true;
        [Tooltip("Margin from screen edge (0-0.5, viewport percentage)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float screenBoundsMargin = 0.15f;

        [Header("Effects")]
        [SerializeField] private float shakeMultiplier = 1f;
        [SerializeField] private float maxShakeOffset = 0.5f;
        [SerializeField] private float maxShakeRotation = 3f;
        [SerializeField] private float shakeTraumaDecay = 1.5f;
        [SerializeField] private float shakeFrequency = 15f;

        public string PresetId => presetId;
        public string DisplayName => displayName;
        public float FollowDampeningMax => followDampeningMax;
        public float FollowDampeningMin => followDampeningMin;
        public float VelocityLookAheadDistance => velocityLookAheadDistance;
        public float VelocityLookAheadSmoothing => velocityLookAheadSmoothing;
        public bool VelocityLookAheadScaleWithZoom => velocityLookAheadScaleWithZoom;
        public float VelocityLookAheadPredictionTime => velocityLookAheadPredictionTime;
        public float FocusLookAheadDistance => focusLookAheadDistance;
        public float FocusLookAheadSmoothing => focusLookAheadSmoothing;
        public AnimationCurve FocusLookAheadSpeedCurve => focusLookAheadSpeedCurve;
        public float MaxLookAheadDistance => maxLookAheadDistance;
        public float MinZoom => minZoom;
        public float MaxZoom => maxZoom;
        public float DefaultZoom => defaultZoom;
        public float ZoomSpeed => zoomSpeed;
        public bool EnableSpeedZoom => enableSpeedZoom;
        public float SpeedZoomMinSpeed => speedZoomMinSpeed;
        public float SpeedZoomMaxSpeed => speedZoomMaxSpeed;
        public AnimationCurve SpeedZoomCurve => speedZoomCurve;
        public float SpeedZoomInfluence => speedZoomInfluence;
        public float ScrollZoomSensitivity => scrollZoomSensitivity;
        public float ScrollZoomSmoothing => scrollZoomSmoothing;
        public bool EnableZoomOvershoot => enableZoomOvershoot;
        public float MaxZoomOvershoot => maxZoomOvershoot;
        public float OvershootRecoverySpeed => overshootRecoverySpeed;
        public float MultiTargetPadding => multiTargetPadding;
        public float MultiTargetMinZoom => multiTargetMinZoom;
        public float MultiTargetMaxZoom => multiTargetMaxZoom;
        public bool EnableScreenBounds => enableScreenBounds;
        public float ScreenBoundsMargin => screenBoundsMargin;
        public float ShakeMultiplier => shakeMultiplier;
        public float MaxShakeOffset => maxShakeOffset;
        public float MaxShakeRotation => maxShakeRotation;
        public float ShakeTraumaDecay => shakeTraumaDecay;
        public float ShakeFrequency => shakeFrequency;

        public CameraPresetInstance CreateInstance()
        {
            return new CameraPresetInstance(this);
        }

#if UNITY_EDITOR
        public static event System.Action<CameraPreset> OnPresetModified;

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
