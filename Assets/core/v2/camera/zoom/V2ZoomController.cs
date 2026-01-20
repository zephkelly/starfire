using Starfire.Core.Cam.Config;
using Starfire.Core.Cam.Zoom;
using Starfire.Core.V2.Cam.Config;
using UnityEngine;

namespace Starfire.Core.V2.Cam
{
    public class V2ZoomController : IZoomController
    {
        private VelocityCameraPresetInstance _preset;

        private float _currentZoom;
        private float _zoomVelocity;

        private float _userZoomLevel;
        private float _targetUserZoom;
        private float _userZoomVelocity;

        private float _speedZoomDelta;

        private float _currentOvershoot;

        private bool _isTransitioning;
        private float _transitionStartZoom;
        private float _transitionTargetZoom;
        private float _transitionDuration;
        private float _transitionElapsed;

        public float CurrentZoom => _currentZoom;
        public float TargetZoom => CalculateTargetZoom();
        public float UserZoomLevel => _userZoomLevel;
        public float SpeedZoomDelta => _speedZoomDelta;

        public V2ZoomController(VelocityCameraPresetInstance preset)
        {
            _preset = preset;
            _userZoomLevel = preset.DefaultZoom;
            _targetUserZoom = preset.DefaultZoom;
            _currentZoom = preset.DefaultZoom;
        }

        public void SetPreset(CameraPresetInstance preset)
        {
            // This interface method is for the old preset type - ignore
        }

        public void SetPreset(VelocityCameraPresetInstance preset)
        {
            _preset = preset;
        }

        public void Update(float speed, float recommendedZoom, float deltaTime)
        {
            UpdateTransition(deltaTime);

            _userZoomLevel = Mathf.SmoothDamp(
                _userZoomLevel,
                _targetUserZoom,
                ref _userZoomVelocity,
                _preset.ScrollZoomSmoothing
            );

            if (_preset.EnableSpeedZoom)
            {
                float normalizedSpeed = Mathf.InverseLerp(
                    _preset.SpeedZoomMinSpeed,
                    _preset.SpeedZoomMaxSpeed,
                    speed
                );

                float curveValue = _preset.SpeedZoomCurve.Evaluate(normalizedSpeed);
                float speedZoomRange = _preset.MaxZoom - _preset.MinZoom;
                _speedZoomDelta = curveValue * speedZoomRange * _preset.SpeedZoomInfluence;
            }
            else
            {
                _speedZoomDelta = 0f;
            }

            float targetZoom;

            if (recommendedZoom > 0)
            {
                targetZoom = Mathf.Clamp(recommendedZoom, _preset.MinZoom, _preset.MaxZoom);
            }
            else
            {
                float baseZoom = _userZoomLevel + _speedZoomDelta;

                if (_preset.EnableZoomOvershoot)
                {
                    float overshootTarget = 0f;

                    if (baseZoom > _preset.MaxZoom)
                    {
                        overshootTarget = Mathf.Min(baseZoom - _preset.MaxZoom, _preset.MaxZoomOvershoot);
                    }
                    else if (baseZoom < _preset.MinZoom)
                    {
                        overshootTarget = Mathf.Max(baseZoom - _preset.MinZoom, -_preset.MaxZoomOvershoot);
                    }

                    if (Mathf.Abs(overshootTarget) > Mathf.Abs(_currentOvershoot))
                    {
                        _currentOvershoot = Mathf.MoveTowards(
                            _currentOvershoot,
                            overshootTarget,
                            _preset.MaxZoomOvershoot * 5f * deltaTime
                        );
                    }
                    else
                    {
                        _currentOvershoot = Mathf.MoveTowards(
                            _currentOvershoot,
                            overshootTarget,
                            _preset.OvershootRecoverySpeed * deltaTime
                        );
                    }

                    targetZoom = Mathf.Clamp(baseZoom, _preset.MinZoom, _preset.MaxZoom) + _currentOvershoot;
                }
                else
                {
                    targetZoom = Mathf.Clamp(baseZoom, _preset.MinZoom, _preset.MaxZoom);
                    _currentOvershoot = 0f;
                }
            }

            _currentZoom = Mathf.SmoothDamp(
                _currentZoom,
                targetZoom,
                ref _zoomVelocity,
                1f / _preset.ZoomSpeed
            );
        }

        private float CalculateTargetZoom()
        {
            float baseZoom = _targetUserZoom + _speedZoomDelta;
            return Mathf.Clamp(baseZoom, _preset.MinZoom, _preset.MaxZoom) + _currentOvershoot;
        }

        private void UpdateTransition(float deltaTime)
        {
            if (!_isTransitioning) return;

            _transitionElapsed += deltaTime;
            float t = Mathf.Clamp01(_transitionElapsed / _transitionDuration);
            t = Mathf.SmoothStep(0, 1, t);

            _targetUserZoom = Mathf.Lerp(_transitionStartZoom, _transitionTargetZoom, t);

            if (t >= 1f)
            {
                _isTransitioning = false;
            }
        }

        public void ScrollZoom(float delta)
        {
            _isTransitioning = false;

            _targetUserZoom -= delta * _preset.ScrollZoomSensitivity;
            _targetUserZoom = Mathf.Clamp(_targetUserZoom, _preset.MinZoom, _preset.MaxZoom);
        }

        public void SetUserZoom(float zoom, float transitionTime = 0.5f)
        {
            float clampedZoom = Mathf.Clamp(zoom, _preset.MinZoom, _preset.MaxZoom);

            if (transitionTime <= 0.01f)
            {
                _targetUserZoom = clampedZoom;
                _userZoomLevel = clampedZoom;
                _isTransitioning = false;
            }
            else
            {
                _transitionStartZoom = _targetUserZoom;
                _transitionTargetZoom = clampedZoom;
                _transitionDuration = transitionTime;
                _transitionElapsed = 0f;
                _isTransitioning = true;
            }
        }

        public void ResetUserZoom(float transitionTime = 0.5f)
        {
            SetUserZoom(_preset.DefaultZoom, transitionTime);
        }
    }
}
