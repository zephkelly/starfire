using UnityEngine;

namespace Starfire.Core.Cam.Config
{
    public class CameraPresetInstance
    {
        private readonly CameraPreset _source;

        public CameraPreset Source => _source;

        public string PresetId { get; set; }
        public string DisplayName { get; set; }
        public float FollowDampeningMax { get; set; }
        public float FollowDampeningMin { get; set; }
        public float VelocityLookAheadDistance { get; set; }
        public float VelocityLookAheadSmoothing { get; set; }
        public bool VelocityLookAheadScaleWithZoom { get; set; }
        public float VelocityLookAheadPredictionTime { get; set; }
        public float FocusLookAheadDistance { get; set; }
        public float FocusLookAheadSmoothing { get; set; }
        public AnimationCurve FocusLookAheadSpeedCurve { get; set; }
        public float MaxLookAheadDistance { get; set; }
        public float MinZoom { get; set; }
        public float MaxZoom { get; set; }
        public float DefaultZoom { get; set; }
        public float ZoomSpeed { get; set; }
        public bool EnableSpeedZoom { get; set; }
        public float SpeedZoomMinSpeed { get; set; }
        public float SpeedZoomMaxSpeed { get; set; }
        public AnimationCurve SpeedZoomCurve { get; set; }
        public float SpeedZoomInfluence { get; set; }
        public float ScrollZoomSensitivity { get; set; }
        public float ScrollZoomSmoothing { get; set; }
        public bool EnableZoomOvershoot { get; set; }
        public float MaxZoomOvershoot { get; set; }
        public float OvershootRecoverySpeed { get; set; }
        public float MultiTargetPadding { get; set; }
        public float MultiTargetMinZoom { get; set; }
        public float MultiTargetMaxZoom { get; set; }
        public bool EnableScreenBounds { get; set; }
        public float ScreenBoundsMargin { get; set; }
        public float ShakeMultiplier { get; set; }
        public float MaxShakeOffset { get; set; }
        public float MaxShakeRotation { get; set; }
        public float ShakeTraumaDecay { get; set; }
        public float ShakeFrequency { get; set; }

        public CameraPresetInstance(CameraPreset source)
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

            PresetId = _source.PresetId;
            DisplayName = _source.DisplayName;
            FollowDampeningMax = _source.FollowDampeningMax;
            FollowDampeningMin = _source.FollowDampeningMin;
            VelocityLookAheadDistance = _source.VelocityLookAheadDistance;
            VelocityLookAheadSmoothing = _source.VelocityLookAheadSmoothing;
            VelocityLookAheadScaleWithZoom = _source.VelocityLookAheadScaleWithZoom;
            VelocityLookAheadPredictionTime = _source.VelocityLookAheadPredictionTime;
            FocusLookAheadDistance = _source.FocusLookAheadDistance;
            FocusLookAheadSmoothing = _source.FocusLookAheadSmoothing;
            FocusLookAheadSpeedCurve = _source.FocusLookAheadSpeedCurve;
            MaxLookAheadDistance = _source.MaxLookAheadDistance;
            MinZoom = _source.MinZoom;
            MaxZoom = _source.MaxZoom;
            DefaultZoom = _source.DefaultZoom;
            ZoomSpeed = _source.ZoomSpeed;
            EnableSpeedZoom = _source.EnableSpeedZoom;
            SpeedZoomMinSpeed = _source.SpeedZoomMinSpeed;
            SpeedZoomMaxSpeed = _source.SpeedZoomMaxSpeed;
            SpeedZoomCurve = _source.SpeedZoomCurve;
            SpeedZoomInfluence = _source.SpeedZoomInfluence;
            ScrollZoomSensitivity = _source.ScrollZoomSensitivity;
            ScrollZoomSmoothing = _source.ScrollZoomSmoothing;
            EnableZoomOvershoot = _source.EnableZoomOvershoot;
            MaxZoomOvershoot = _source.MaxZoomOvershoot;
            OvershootRecoverySpeed = _source.OvershootRecoverySpeed;
            MultiTargetPadding = _source.MultiTargetPadding;
            MultiTargetMinZoom = _source.MultiTargetMinZoom;
            MultiTargetMaxZoom = _source.MultiTargetMaxZoom;
            EnableScreenBounds = _source.EnableScreenBounds;
            ScreenBoundsMargin = _source.ScreenBoundsMargin;
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

        public void LerpTo(CameraPresetInstance target, float t)
        {
            FollowDampeningMax = Mathf.Lerp(FollowDampeningMax, target.FollowDampeningMax, t);
            FollowDampeningMin = Mathf.Lerp(FollowDampeningMin, target.FollowDampeningMin, t);
            VelocityLookAheadDistance = Mathf.Lerp(VelocityLookAheadDistance, target.VelocityLookAheadDistance, t);
            VelocityLookAheadSmoothing = Mathf.Lerp(VelocityLookAheadSmoothing, target.VelocityLookAheadSmoothing, t);
            VelocityLookAheadPredictionTime = Mathf.Lerp(VelocityLookAheadPredictionTime, target.VelocityLookAheadPredictionTime, t);
            FocusLookAheadDistance = Mathf.Lerp(FocusLookAheadDistance, target.FocusLookAheadDistance, t);
            FocusLookAheadSmoothing = Mathf.Lerp(FocusLookAheadSmoothing, target.FocusLookAheadSmoothing, t);
            MaxLookAheadDistance = Mathf.Lerp(MaxLookAheadDistance, target.MaxLookAheadDistance, t);
            MinZoom = Mathf.Lerp(MinZoom, target.MinZoom, t);
            MaxZoom = Mathf.Lerp(MaxZoom, target.MaxZoom, t);
            DefaultZoom = Mathf.Lerp(DefaultZoom, target.DefaultZoom, t);
            ZoomSpeed = Mathf.Lerp(ZoomSpeed, target.ZoomSpeed, t);
            SpeedZoomMinSpeed = Mathf.Lerp(SpeedZoomMinSpeed, target.SpeedZoomMinSpeed, t);
            SpeedZoomMaxSpeed = Mathf.Lerp(SpeedZoomMaxSpeed, target.SpeedZoomMaxSpeed, t);
            SpeedZoomInfluence = Mathf.Lerp(SpeedZoomInfluence, target.SpeedZoomInfluence, t);
            ScrollZoomSensitivity = Mathf.Lerp(ScrollZoomSensitivity, target.ScrollZoomSensitivity, t);
            ScrollZoomSmoothing = Mathf.Lerp(ScrollZoomSmoothing, target.ScrollZoomSmoothing, t);
            MaxZoomOvershoot = Mathf.Lerp(MaxZoomOvershoot, target.MaxZoomOvershoot, t);
            OvershootRecoverySpeed = Mathf.Lerp(OvershootRecoverySpeed, target.OvershootRecoverySpeed, t);
            MultiTargetPadding = Mathf.Lerp(MultiTargetPadding, target.MultiTargetPadding, t);
            MultiTargetMinZoom = Mathf.Lerp(MultiTargetMinZoom, target.MultiTargetMinZoom, t);
            MultiTargetMaxZoom = Mathf.Lerp(MultiTargetMaxZoom, target.MultiTargetMaxZoom, t);
            ScreenBoundsMargin = Mathf.Lerp(ScreenBoundsMargin, target.ScreenBoundsMargin, t);
            ShakeMultiplier = Mathf.Lerp(ShakeMultiplier, target.ShakeMultiplier, t);
            MaxShakeOffset = Mathf.Lerp(MaxShakeOffset, target.MaxShakeOffset, t);
            MaxShakeRotation = Mathf.Lerp(MaxShakeRotation, target.MaxShakeRotation, t);
            ShakeTraumaDecay = Mathf.Lerp(ShakeTraumaDecay, target.ShakeTraumaDecay, t);
            ShakeFrequency = Mathf.Lerp(ShakeFrequency, target.ShakeFrequency, t);
        }
    }
}
