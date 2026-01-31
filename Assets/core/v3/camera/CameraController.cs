using Starfire.Core.Cam;
using Starfire.Core.V3.Cam.Config;
using Starfire.Core.V3.Cam.Effects;
using StarfireV2;
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
        private float _zoomContinuousInput; // held value (d-pad): persists until canceled
        private float _zoomImpulseInput;    // impulse value (scroll): consumed each frame
        private IInputProvider _inputProvider;
        private bool _isUsingGamepad;

        // Effects
        private V3CameraEffectsManager _effectsManager;
        private V3CameraShakeService _shakeService;

        // Wake effect
        private static readonly int WakeCenterPositionId = Shader.PropertyToID("_WakeCenterPosition");
        private static readonly int WakeOrthoSizeId = Shader.PropertyToID("_WakeOrthoSize");

        // Starfield shader globals
        private static readonly int CameraOrthoSizeId = Shader.PropertyToID("_CameraOrthoSize");
        private static readonly int CameraWorldPosId = Shader.PropertyToID("_CameraWorldPos");
        private static readonly int ScreenAspectId = Shader.PropertyToID("_ScreenAspect");

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _targetZoom = _camera.orthographicSize;

            // Initialize effects system
            _effectsManager = new V3CameraEffectsManager();
            _shakeService = gameObject.AddComponent<V3CameraShakeService>();
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

                // Configure effects from preset
                if (preset.screenShakeConfig != null)
                {
                    _effectsManager.SetConfig(preset.screenShakeConfig);
                }
            }

            // Initialize shake service with references
            _shakeService.Initialize(_effectsManager, _camera);

            // Subscribe to input provider for zoom
            if (InputProviderRegistry.Instance != null)
            {
                _inputProvider = InputProviderRegistry.Instance.GetProvider();
                if (_inputProvider != null)
                {
                    _inputProvider.OnZoom += HandleZoom;
                    _inputProvider.OnInputDeviceChanged += HandleDeviceChanged;
                }
            }
        }

        private void OnDestroy()
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnZoom -= HandleZoom;
                _inputProvider.OnInputDeviceChanged -= HandleDeviceChanged;
            }
        }

        private void HandleDeviceChanged(bool isGamepad)
        {
            _isUsingGamepad = isGamepad;
        }

        private void HandleZoom(float value)
        {
            if (_isUsingGamepad)
            {
                // D-pad: ±1 on press, 0 on release
                _zoomContinuousInput = Mathf.Abs(value) < 0.001f ? 0f : value;
            }
            else
            {
                // Mouse scroll: impulse per tick, ignore zero (PassThrough noise)
                if (Mathf.Abs(value) > 0.001f)
                    _zoomImpulseInput += value;
            }
        }

        private void TrySubscribeZoom()
        {
            if (_inputProvider != null) return;
            if (InputProviderRegistry.Instance == null) return;
            _inputProvider = InputProviderRegistry.Instance.GetProvider();
            if (_inputProvider != null)
            {
                _inputProvider.OnZoom += HandleZoom;
                _inputProvider.OnInputDeviceChanged += HandleDeviceChanged;
            }
        }

        private void LateUpdate()
        {
            if (_inputProvider == null) TrySubscribeZoom();
            if (_target == null || !_target.IsValid) return;

            Vector2 pos = _target.Position;

            // Aim offset
            if (preset != null && preset.maxLookAhead > 0f)
            {
                Vector2 targetAimOffset = Vector2.zero;

                if (_target.HasFocus)
                {
                    // Use raw (unnormalized) focus direction — avoids WorldToScreenPoint feedback loop
                    Vector2 focusRaw = _target.FocusDirectionRaw;
                    float aimDist = focusRaw.magnitude;

                    if (aimDist > 0.001f)
                    {
                        // Map aim distance to curve using ortho size as reference scale
                        float refDist = _camera.orthographicSize * 2f;
                        float normalizedDist = aimDist / refDist;
                        float t = Mathf.InverseLerp(preset.innerLimit, preset.outerLimit, normalizedDist);
                        t = Mathf.Clamp01(t);
                        float curveValue = preset.lookAheadCurve.Evaluate(t);
                        float lookAhead = Mathf.Lerp(preset.minLookAhead, preset.maxLookAhead, curveValue);
                        targetAimOffset = (focusRaw / aimDist) * lookAhead;
                    }
                }
                else
                {
                    Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    Vector2 mouseOffset = (Vector2)Input.mousePosition - screenCenter;
                    float halfScreen = Mathf.Min(Screen.width, Screen.height) * 0.5f;
                    float normalizedDist = mouseOffset.magnitude / halfScreen;
                    float t = Mathf.InverseLerp(preset.innerLimit, preset.outerLimit, normalizedDist);
                    t = Mathf.Clamp01(t);
                    float curveValue = preset.lookAheadCurve.Evaluate(t);
                    float lookAhead = Mathf.Lerp(preset.minLookAhead, preset.maxLookAhead, curveValue);
                    if (lookAhead > 0f && mouseOffset.sqrMagnitude > 0.001f)
                    {
                        targetAimOffset = mouseOffset.normalized * lookAhead;
                    }
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

            // Update and apply camera effects (shake, punch)
            _effectsManager.Update(Time.deltaTime);

            Vector3 finalPos = new Vector3(pos.x, pos.y, transform.position.z);
            float rotation = 0f;
            _effectsManager.ApplyEffects(ref finalPos, ref rotation);

            transform.position = finalPos;
            transform.rotation = Quaternion.Euler(0f, 0f, rotation);

            // Zoom
            UpdateZoom();

            // Update wake effect position
            UpdateWakePosition();

            // Update starfield shader globals
            UpdateStarfieldGlobals();
        }

        private void UpdateStarfieldGlobals()
        {
            Shader.SetGlobalFloat(CameraOrthoSizeId, _camera.orthographicSize);
            Shader.SetGlobalVector(CameraWorldPosId, new Vector4(transform.position.x, transform.position.y, 0, 0));
            Shader.SetGlobalFloat(ScreenAspectId, _camera.aspect);
        }

        private void UpdateWakePosition()
        {
            // Ship is offset from screen center by the inverse of aim offset
            Vector2 aimOffsetScreen = _currentAimOffset / (_camera.orthographicSize * 2f);
            aimOffsetScreen.x /= _camera.aspect;

            Vector2 shipScreenPos = new Vector2(0.5f, 0.5f) - aimOffsetScreen;
            Shader.SetGlobalVector(WakeCenterPositionId, shipScreenPos);

            // Pass orthographic size for wake effect zoom scaling
            Shader.SetGlobalFloat(WakeOrthoSizeId, _camera.orthographicSize);
        }

        private void UpdateZoom()
        {
            if (preset == null) return;

            // Combine continuous (d-pad) + impulse (scroll) with separate sensitivities
            float scrollInput = _zoomContinuousInput * preset.gamepadZoomSpeed * Time.deltaTime
                              + _zoomImpulseInput * preset.scrollSensitivity;
            _zoomImpulseInput = 0f;

            if (Mathf.Abs(scrollInput) > 0.001f)
            {
                // Calculate rate multiplier based on current zoom level
                float normalizedZoom = Mathf.InverseLerp(preset.minZoom, preset.maxZoom, _targetZoom);
                float rateMultiplier = preset.zoomRateCurve.Evaluate(normalizedZoom);

                // Apply zoom (negative because scroll up / d-pad up = zoom in = smaller ortho size)
                float zoomDelta = -scrollInput * rateMultiplier;
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
