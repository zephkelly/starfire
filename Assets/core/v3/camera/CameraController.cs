using Starfire.Core.Cam;
using Starfire.Core.V3.Cam.Config;
using UnityEngine;

namespace Starfire.Core.V3.Cam
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CameraPreset preset;
        [SerializeField] private EntityCameraTarget target;
        [SerializeField] private bool autoFindTarget = true;

        private Camera _camera;
        private ICameraTarget _target;

        // Smoothed aim offset
        private Vector2 _currentAimOffset;
        private Vector2 _aimOffsetVelocity;

        // Zoom
        private float _targetZoom;
        private float _zoomVelocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _targetZoom = _camera.orthographicSize;
        }

        private void Start()
        {
            if (target != null)
            {
                _target = target;
            }
            else if (autoFindTarget)
            {
                var found = FindFirstObjectByType<EntityCameraTarget>();
                if (found != null)
                {
                    _target = found;
                }
            }

            if (preset != null)
            {
                _targetZoom = preset.orthographicSize;
                _camera.orthographicSize = _targetZoom;
            }
        }

        private void LateUpdate()
        {
            if (_target == null || !_target.IsValid) return;

            Vector2 pos = _target.Position;

            // Aim offset: use screen center (entity is always centered)
            if (preset != null && preset.maxLookAhead > 0f)
            {
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 mouseOffset = (Vector2)Input.mousePosition - screenCenter;

                // Normalize distance to half-screen (use smaller dimension for consistent feel)
                float halfScreen = Mathf.Min(Screen.width, Screen.height) * 0.5f;
                float normalizedDist = mouseOffset.magnitude / halfScreen;

                // Map to inner/outer limits
                float t = Mathf.InverseLerp(preset.innerLimit, preset.outerLimit, normalizedDist);
                t = Mathf.Clamp01(t);

                // Apply curve and calculate final look-ahead
                float curveValue = preset.lookAheadCurve.Evaluate(t);
                float lookAhead = Mathf.Lerp(preset.minLookAhead, preset.maxLookAhead, curveValue);

                // Calculate target aim offset
                Vector2 targetAimOffset = Vector2.zero;
                if (lookAhead > 0f && mouseOffset.sqrMagnitude > 0.001f)
                {
                    targetAimOffset = mouseOffset.normalized * lookAhead;
                }

                // Smooth the aim offset
                if (preset.lookAheadSmoothing > 0f)
                {
                    _currentAimOffset = Vector2.SmoothDamp(
                        _currentAimOffset,
                        targetAimOffset,
                        ref _aimOffsetVelocity,
                        preset.lookAheadSmoothing
                    );
                }
                else
                {
                    _currentAimOffset = targetAimOffset;
                }

                pos += _currentAimOffset;
            }

            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            // Zoom
            UpdateZoom();
        }

        private void UpdateZoom()
        {
            if (preset == null) return;

            float scrollInput = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollInput) > 0.001f)
            {
                // Calculate rate multiplier based on current zoom level
                float normalizedZoom = Mathf.InverseLerp(preset.minZoom, preset.maxZoom, _targetZoom);
                float rateMultiplier = preset.zoomRateCurve.Evaluate(normalizedZoom);

                // Apply scroll (negative because scroll up = zoom in = smaller ortho size)
                float zoomDelta = -scrollInput * preset.scrollSensitivity * rateMultiplier;
                float newTargetZoom = Mathf.Clamp(_targetZoom + zoomDelta, preset.minZoom, preset.maxZoom);

                // Reset velocity if zoom direction changes to prevent fighting
                float currentDirection = _targetZoom - _camera.orthographicSize;
                float newDirection = newTargetZoom - _camera.orthographicSize;
                if (currentDirection * newDirection < 0f)
                {
                    _zoomVelocity = 0f;
                }

                _targetZoom = newTargetZoom;
            }

            // Apply zoom - separate smooth vs instant paths
            if (preset.zoomSmoothing > 0f)
            {
                float diff = Mathf.Abs(_camera.orthographicSize - _targetZoom);
                if (diff > 0.001f)
                {
                    _camera.orthographicSize = Mathf.SmoothDamp(
                        _camera.orthographicSize,
                        _targetZoom,
                        ref _zoomVelocity,
                        preset.zoomSmoothing
                    );
                }
                else
                {
                    _camera.orthographicSize = _targetZoom;
                    _zoomVelocity = 0f;
                }
            }
            else
            {
                _camera.orthographicSize = _targetZoom;
            }
        }

        public void SetTarget(ICameraTarget newTarget)
        {
            _target = newTarget;
        }
    }
}
