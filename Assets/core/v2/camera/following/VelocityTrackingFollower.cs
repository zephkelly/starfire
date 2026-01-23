using Starfire.Core.V2.Cam.Config;
using UnityEngine;

namespace Starfire.Core.V2.Cam.Following
{
    public class VelocityTrackingFollower : IFollowStrategy
    {
        private VelocityCameraPresetInstance _preset;

        private Vector2 _trackedPosition;
        private Vector2 _cameraPosition;
        private Vector2 _cameraVelocity;
        private Vector2 _smoothDampVelocity;
        private Vector2 _focusOffset;
        private Vector2 _focusOffsetVelocity;
        private float _trackingFactor;
        private Vector2 _positionError;

        private const float MinDampening = 0.001f;

        public Vector2 CurrentPosition => _cameraPosition;
        public Vector2 CurrentVelocity => _cameraVelocity;
        public float TrackingFactor => _trackingFactor;
        public Vector2 PositionError => _positionError;
        public Vector2 FocusOffset => _focusOffset;

        public VelocityTrackingFollower(VelocityCameraPresetInstance preset)
        {
            _preset = preset;
        }

        public void SetPreset(VelocityCameraPresetInstance preset)
        {
            _preset = preset;
        }

        public void Initialize(Vector2 startPosition)
        {
            _trackedPosition = startPosition;
            _cameraPosition = startPosition;
            _cameraVelocity = Vector2.zero;
            _smoothDampVelocity = Vector2.zero;
            _focusOffset = Vector2.zero;
            _focusOffsetVelocity = Vector2.zero;
            _trackingFactor = 0f;
            _positionError = Vector2.zero;
        }

        public Vector2 Update(
            Vector2 targetPosition,
            Vector2 targetVelocity,
            Vector2 focusDirection,
            float currentZoom,
            float deltaTime)
        {
            if (_preset == null) return _cameraPosition;

            float speed = targetVelocity.magnitude;

            // 1. Calculate adaptive dampening (decreases as speed increases)
            float speedNormalized = Mathf.Clamp01(speed / _preset.VelocityTrackingFullSpeed);
            float dampening = Mathf.Lerp(
                _preset.LowSpeedDampening,
                MinDampening,
                speedNormalized
            );
            _trackingFactor = speedNormalized;

            // 2. Track position with adaptive dampening
            _trackedPosition = Vector2.SmoothDamp(
                _trackedPosition,
                targetPosition,
                ref _smoothDampVelocity,
                dampening
            );

            // 3. Calculate and smooth focus offset separately
            Vector2 desiredFocusOffset = CalculateFocusOffset(focusDirection, speed, currentZoom);
            _focusOffset = Vector2.SmoothDamp(
                _focusOffset,
                desiredFocusOffset,
                ref _focusOffsetVelocity,
                _preset.FocusSmoothing
            );

            // 4. Final position = tracked position + focus offset
            _cameraPosition = _trackedPosition + _focusOffset;
            _cameraVelocity = _smoothDampVelocity;
            _positionError = targetPosition - _trackedPosition;

            return _cameraPosition;
        }

        private Vector2 CalculateFocusOffset(Vector2 focusDirection, float speed, float currentZoom)
        {
            if (focusDirection.sqrMagnitude < 0.01f)
                return Vector2.zero;

            // Apply speed influence curve (reduces focus at high speed)
            float normalizedSpeed = speed / Mathf.Max(_preset.VelocityTrackingFullSpeed, 0.001f);
            float influence = _preset.FocusSpeedInfluence.Evaluate(Mathf.Clamp01(normalizedSpeed));

            float distance = _preset.FocusLookAheadDistance * influence;

            // Scale with zoom for consistent screen-space offset
            if (_preset.ScaleFocusWithZoom && _preset.DefaultZoom > 0)
            {
                distance *= currentZoom / _preset.DefaultZoom;
            }

            return focusDirection.normalized * distance;
        }

        public void SnapTo(Vector2 position)
        {
            _trackedPosition = position;
            _cameraPosition = position;
            _cameraVelocity = Vector2.zero;
            _smoothDampVelocity = Vector2.zero;
            _focusOffset = Vector2.zero;
            _focusOffsetVelocity = Vector2.zero;
            _positionError = Vector2.zero;
        }

        public void ShiftPosition(Vector2 shiftAmount)
        {
            _trackedPosition += shiftAmount;
            _cameraPosition += shiftAmount;
        }
    }
}
