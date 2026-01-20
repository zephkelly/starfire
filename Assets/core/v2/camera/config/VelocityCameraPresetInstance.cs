using UnityEngine;

namespace Starfire.Core.V2.Cam.Config
{
    public class VelocityCameraPresetInstance
    {
        private readonly VelocityCameraPreset _source;

        public VelocityCameraPreset Source => _source;

        // Identity
        public string PresetId { get; set; }
        public string DisplayName { get; set; }

        // Velocity Tracking
        public float VelocityTrackingThreshold { get; set; }
        public float VelocityTrackingFullSpeed { get; set; }
        public float PositionCorrectionTime { get; set; }
        public float MaxPositionError { get; set; }
        public AnimationCurve VelocityTrackingCurve { get; set; }

        // Low Speed Following
        public float LowSpeedDampening { get; set; }

        // Focus Look-Ahead
        public float FocusLookAheadDistance { get; set; }
        public float FocusSmoothing { get; set; }
        public bool ScaleFocusWithZoom { get; set; }
        public AnimationCurve FocusSpeedInfluence { get; set; }

        // Zoom
        public float MinZoom { get; set; }
        public float MaxZoom { get; set; }
        public float DefaultZoom { get; set; }
        public float ZoomSpeed { get; set; }

        // Speed-Based Zoom
        public bool EnableSpeedZoom { get; set; }
        public float SpeedZoomMinSpeed { get; set; }
        public float SpeedZoomMaxSpeed { get; set; }
        public AnimationCurve SpeedZoomCurve { get; set; }
        public float SpeedZoomInfluence { get; set; }

        // Scroll Zoom
        public float ScrollZoomSensitivity { get; set; }
        public float ScrollZoomSmoothing { get; set; }

        // Zoom Overshoot
        public bool EnableZoomOvershoot { get; set; }
        public float MaxZoomOvershoot { get; set; }
        public float OvershootRecoverySpeed { get; set; }

        // Multi-Target
        public float MultiTargetPadding { get; set; }
        public float MultiTargetMinZoom { get; set; }
        public float MultiTargetMaxZoom { get; set; }

        // Screen Bounds
        public bool EnableScreenBounds { get; set; }
        public float ScreenBoundsMargin { get; set; }

        // Effects
        public float ShakeMultiplier { get; set; }
        public float MaxShakeOffset { get; set; }
        public float MaxShakeRotation { get; set; }
        public float ShakeTraumaDecay { get; set; }
        public float ShakeFrequency { get; set; }

        public VelocityCameraPresetInstance(VelocityCameraPreset source)
        {
            _source = source;
            if (_source != null)
            {
                CopyFromSource();
            }
        }

        public void CopyFromSource()
        {
            if (_source == null) return;

            // Identity
            PresetId = _source.PresetId;
            DisplayName = _source.DisplayName;

            // Velocity Tracking
            VelocityTrackingThreshold = _source.VelocityTrackingThreshold;
            VelocityTrackingFullSpeed = _source.VelocityTrackingFullSpeed;
            PositionCorrectionTime = _source.PositionCorrectionTime;
            MaxPositionError = _source.MaxPositionError;
            VelocityTrackingCurve = _source.VelocityTrackingCurve;

            // Low Speed Following
            LowSpeedDampening = _source.LowSpeedDampening;

            // Focus Look-Ahead
            FocusLookAheadDistance = _source.FocusLookAheadDistance;
            FocusSmoothing = _source.FocusSmoothing;
            ScaleFocusWithZoom = _source.ScaleFocusWithZoom;
            FocusSpeedInfluence = _source.FocusSpeedInfluence;

            // Zoom
            MinZoom = _source.MinZoom;
            MaxZoom = _source.MaxZoom;
            DefaultZoom = _source.DefaultZoom;
            ZoomSpeed = _source.ZoomSpeed;

            // Speed-Based Zoom
            EnableSpeedZoom = _source.EnableSpeedZoom;
            SpeedZoomMinSpeed = _source.SpeedZoomMinSpeed;
            SpeedZoomMaxSpeed = _source.SpeedZoomMaxSpeed;
            SpeedZoomCurve = _source.SpeedZoomCurve;
            SpeedZoomInfluence = _source.SpeedZoomInfluence;

            // Scroll Zoom
            ScrollZoomSensitivity = _source.ScrollZoomSensitivity;
            ScrollZoomSmoothing = _source.ScrollZoomSmoothing;

            // Zoom Overshoot
            EnableZoomOvershoot = _source.EnableZoomOvershoot;
            MaxZoomOvershoot = _source.MaxZoomOvershoot;
            OvershootRecoverySpeed = _source.OvershootRecoverySpeed;

            // Multi-Target
            MultiTargetPadding = _source.MultiTargetPadding;
            MultiTargetMinZoom = _source.MultiTargetMinZoom;
            MultiTargetMaxZoom = _source.MultiTargetMaxZoom;

            // Screen Bounds
            EnableScreenBounds = _source.EnableScreenBounds;
            ScreenBoundsMargin = _source.ScreenBoundsMargin;

            // Effects
            ShakeMultiplier = _source.ShakeMultiplier;
            MaxShakeOffset = _source.MaxShakeOffset;
            MaxShakeRotation = _source.MaxShakeRotation;
            ShakeTraumaDecay = _source.ShakeTraumaDecay;
            ShakeFrequency = _source.ShakeFrequency;
        }

        public void RefreshFromSource()
        {
            if (_source != null)
            {
                CopyFromSource();
            }
        }

        public void LerpTo(VelocityCameraPresetInstance target, float t)
        {
            // Velocity Tracking
            VelocityTrackingThreshold = Mathf.Lerp(VelocityTrackingThreshold, target.VelocityTrackingThreshold, t);
            VelocityTrackingFullSpeed = Mathf.Lerp(VelocityTrackingFullSpeed, target.VelocityTrackingFullSpeed, t);
            PositionCorrectionTime = Mathf.Lerp(PositionCorrectionTime, target.PositionCorrectionTime, t);
            MaxPositionError = Mathf.Lerp(MaxPositionError, target.MaxPositionError, t);

            // Low Speed Following
            LowSpeedDampening = Mathf.Lerp(LowSpeedDampening, target.LowSpeedDampening, t);

            // Focus Look-Ahead
            FocusLookAheadDistance = Mathf.Lerp(FocusLookAheadDistance, target.FocusLookAheadDistance, t);
            FocusSmoothing = Mathf.Lerp(FocusSmoothing, target.FocusSmoothing, t);

            // Zoom
            MinZoom = Mathf.Lerp(MinZoom, target.MinZoom, t);
            MaxZoom = Mathf.Lerp(MaxZoom, target.MaxZoom, t);
            DefaultZoom = Mathf.Lerp(DefaultZoom, target.DefaultZoom, t);
            ZoomSpeed = Mathf.Lerp(ZoomSpeed, target.ZoomSpeed, t);

            // Speed-Based Zoom
            SpeedZoomMinSpeed = Mathf.Lerp(SpeedZoomMinSpeed, target.SpeedZoomMinSpeed, t);
            SpeedZoomMaxSpeed = Mathf.Lerp(SpeedZoomMaxSpeed, target.SpeedZoomMaxSpeed, t);
            SpeedZoomInfluence = Mathf.Lerp(SpeedZoomInfluence, target.SpeedZoomInfluence, t);

            // Scroll Zoom
            ScrollZoomSensitivity = Mathf.Lerp(ScrollZoomSensitivity, target.ScrollZoomSensitivity, t);
            ScrollZoomSmoothing = Mathf.Lerp(ScrollZoomSmoothing, target.ScrollZoomSmoothing, t);

            // Zoom Overshoot
            MaxZoomOvershoot = Mathf.Lerp(MaxZoomOvershoot, target.MaxZoomOvershoot, t);
            OvershootRecoverySpeed = Mathf.Lerp(OvershootRecoverySpeed, target.OvershootRecoverySpeed, t);

            // Multi-Target
            MultiTargetPadding = Mathf.Lerp(MultiTargetPadding, target.MultiTargetPadding, t);
            MultiTargetMinZoom = Mathf.Lerp(MultiTargetMinZoom, target.MultiTargetMinZoom, t);
            MultiTargetMaxZoom = Mathf.Lerp(MultiTargetMaxZoom, target.MultiTargetMaxZoom, t);

            // Screen Bounds
            ScreenBoundsMargin = Mathf.Lerp(ScreenBoundsMargin, target.ScreenBoundsMargin, t);

            // Effects
            ShakeMultiplier = Mathf.Lerp(ShakeMultiplier, target.ShakeMultiplier, t);
            MaxShakeOffset = Mathf.Lerp(MaxShakeOffset, target.MaxShakeOffset, t);
            MaxShakeRotation = Mathf.Lerp(MaxShakeRotation, target.MaxShakeRotation, t);
            ShakeTraumaDecay = Mathf.Lerp(ShakeTraumaDecay, target.ShakeTraumaDecay, t);
            ShakeFrequency = Mathf.Lerp(ShakeFrequency, target.ShakeFrequency, t);
        }
    }
}
