using System;
using System.Collections.Generic;
using Starfire.Core.Cam.Config;
using Starfire.Core.Cam.Effects;
using Starfire.Core.Cam.Targeting;
using Starfire.Core.Cam.Zoom;
using UnityEngine;
using UnityEngine.Rendering;

namespace Starfire.Core.Cam
{
    public enum CameraUpdateMode
    {
        LateUpdate,
        FixedUpdate,
        Auto
    }

    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CameraPreset defaultPreset;

        [Header("Initial Target")]
        [SerializeField] private EntityCameraTarget initialTarget;
        [SerializeField] private bool autoFindTarget = true;
        [SerializeField] private bool snapOnStart = true;

        [Header("Update Mode")]
        [SerializeField] private CameraUpdateMode updateMode = CameraUpdateMode.Auto;
        [SerializeField] private bool useInterpolation = true;

        [Header("URP Integration")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private bool autoFindVolume = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo;

        [Header("Debug Controls")]
        [SerializeField] private bool enableDebugControls = true;
        [SerializeField] private float debugShakeIntensity = 0.5f;
        [SerializeField] private float debugPunchForce = 0.3f;
        [SerializeField] private float debugChromaticIntensity = 0.5f;
        [SerializeField] private float debugVignetteIntensity = 0.3f;

        public UnityEngine.Camera Camera { get; private set; }
        public CameraPresetInstance CurrentPreset { get; private set; }
        public CameraUpdateMode UpdateMode => updateMode;

        public CameraUpdateMode EffectiveUpdateMode
        {
            get
            {
                if (updateMode != CameraUpdateMode.Auto)
                    return updateMode;

                return IsPhysicsTarget() ? CameraUpdateMode.FixedUpdate : CameraUpdateMode.LateUpdate;
            }
        }

        private ICameraTargetProvider _targetProvider;
        private IZoomController _zoomController;
        private CameraEffectsManager _effectsManager;

        private Vector2 _currentPosition;
        private Vector2 _positionVelocity;
        private Vector2 _velocityLookAhead;
        private Vector2 _velocityLookAheadVelocity;
        private Vector2 _focusLookAhead;
        private Vector2 _focusLookAheadVelocity;
        private float _currentZoom;
        private float _baseRotation;

        private bool _isTransitioningPreset;
        private CameraPresetInstance _transitionTargetPreset;
        private float _presetTransitionDuration;
        private float _presetTransitionElapsed;

        private bool _updatedThisFrame;

        // Wake effect shader globals
        private static readonly int WakeCenterPositionId = Shader.PropertyToID("_WakeCenterPosition");
        private static readonly int WakeOrthoSizeId = Shader.PropertyToID("_WakeOrthoSize");

        public event Action<CameraPreset> OnPresetChanged;
        public event Action<ICameraTarget> OnTargetChanged;

        private void Awake()
        {
            Camera = GetComponent<UnityEngine.Camera>();
            _effectsManager = new CameraEffectsManager(this);
            _targetProvider = new SingleTargetProvider();

            Volume volume = postProcessVolume;
            if (volume == null && autoFindVolume)
            {
                volume = FindFirstObjectByType<Volume>();
            }

            if (volume != null)
            {
                _effectsManager.SetPostProcessingBridge(new PostProcessingBridge(volume));
            }

            if (defaultPreset != null)
            {
                ApplyPreset(defaultPreset, immediate: true);
            }
            else
            {
                CreateDefaultPreset();
            }

            _currentPosition = transform.position;
            _currentZoom = Camera.orthographicSize;
            _baseRotation = transform.eulerAngles.z;

#if UNITY_EDITOR
            CameraPreset.OnPresetModified += OnPresetAssetModified;
#endif
        }

        private void Start()
        {
            ICameraTarget target = initialTarget;

            if (target == null && autoFindTarget)
            {
                target = FindFirstObjectByType<EntityCameraTarget>();
            }

            if (target != null)
            {
                SetTarget(target);

                if (snapOnStart)
                {
                    SnapToTarget();
                }
            }
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            CameraPreset.OnPresetModified -= OnPresetAssetModified;
        }

        private void OnPresetAssetModified(CameraPreset preset)
        {
            if (CurrentPreset != null && CurrentPreset.Source == preset)
            {
                CurrentPreset.RefreshFromSource();
                _zoomController?.SetPreset(CurrentPreset);
                _effectsManager?.SetPreset(CurrentPreset);
            }
        }
#endif

        private void CreateDefaultPreset()
        {
            CurrentPreset = new CameraPresetInstance(null)
            {
                FollowDampeningMax = 0.1f,
                FollowDampeningMin = 0.02f,
                VelocityLookAheadDistance = 2f,
                VelocityLookAheadSmoothing = 0.15f,
                VelocityLookAheadScaleWithZoom = true,
                VelocityLookAheadPredictionTime = 0.3f,
                FocusLookAheadDistance = 1.5f,
                FocusLookAheadSmoothing = 0.3f,
                MinZoom = 5f,
                MaxZoom = 15f,
                DefaultZoom = 10f,
                ZoomSpeed = 2f,
                EnableSpeedZoom = true,
                SpeedZoomMinSpeed = 0f,
                SpeedZoomMaxSpeed = 20f,
                SpeedZoomCurve = AnimationCurve.Linear(0, 0, 1, 1),
                SpeedZoomInfluence = 0.5f,
                ScrollZoomSensitivity = 1f,
                ScrollZoomSmoothing = 0.15f,
                EnableZoomOvershoot = true,
                MaxZoomOvershoot = 2f,
                OvershootRecoverySpeed = 3f,
                MultiTargetPadding = 3f,
                MultiTargetMinZoom = 5f,
                MultiTargetMaxZoom = 20f,
                EnableScreenBounds = true,
                ScreenBoundsMargin = 0.15f,
                ShakeMultiplier = 1f,
                MaxShakeOffset = 0.5f,
                MaxShakeRotation = 3f,
                ShakeTraumaDecay = 1.5f,
                ShakeFrequency = 15f
            };

            _zoomController = new SpeedBasedZoom(CurrentPreset);
            _effectsManager.SetPreset(CurrentPreset);
        }

        private void Update()
        {
            _updatedThisFrame = false;

            // Handle scroll wheel zoom
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                _zoomController.ScrollZoom(scrollDelta);
            }

            // Debug controls
            if (enableDebugControls)
            {
                HandleDebugControls();
            }
        }

        private void FixedUpdate()
        {
            var effectiveMode = EffectiveUpdateMode;
            if (effectiveMode == CameraUpdateMode.FixedUpdate)
            {
                UpdateCamera(Time.fixedDeltaTime, useInterpolatedPosition: false);
                _updatedThisFrame = true;
            }
        }

        private void LateUpdate()
        {
            if (_updatedThisFrame) return;

            var effectiveMode = EffectiveUpdateMode;
            if (effectiveMode == CameraUpdateMode.LateUpdate)
            {
                UpdateCamera(Time.deltaTime, useInterpolatedPosition: useInterpolation);
            }
        }

        private void UpdateCamera(float deltaTime, bool useInterpolatedPosition)
        {
            UpdatePresetTransition(deltaTime);

            if (!_targetProvider.HasTargets)
            {
                _effectsManager.Update(deltaTime);
                return;
            }

            float speed = _targetProvider.GetTargetVelocity().magnitude;
            Vector2 targetPos = CalculateTargetPosition(useInterpolatedPosition, speed);

            // Apply screen bounds constraint to keep target visible
            Vector2 actualTargetPos = useInterpolatedPosition
                ? _targetProvider.GetInterpolatedPosition()
                : _targetProvider.GetTargetPosition();
            targetPos = ApplyScreenBoundsConstraint(targetPos, actualTargetPos);

            float adaptiveDampening = GetAdaptiveFollowDampening(speed);
            _currentPosition = Vector2.SmoothDamp(
                _currentPosition,
                targetPos,
                ref _positionVelocity,
                adaptiveDampening
            );
            float recommendedZoom = _targetProvider.GetRecommendedZoom();
            _zoomController.Update(speed, recommendedZoom, deltaTime);
            _currentZoom = _zoomController.CurrentZoom;

            _effectsManager.Update(deltaTime);

            Vector3 finalPos = new Vector3(_currentPosition.x, _currentPosition.y, transform.position.z);
            float finalRotation = _baseRotation;
            float finalZoom = _currentZoom;

            _effectsManager.ApplyAllEffects(ref finalPos, ref finalRotation, ref finalZoom);

            transform.position = finalPos;
            transform.rotation = Quaternion.Euler(0, 0, finalRotation);
            Camera.orthographicSize = finalZoom;

            // Update wake center position in viewport space
            if (_targetProvider.HasTargets)
            {
                Vector2 targetWorldPos = useInterpolatedPosition
                    ? _targetProvider.GetInterpolatedPosition()
                    : _targetProvider.GetTargetPosition();
                Vector3 viewportPos = Camera.WorldToViewportPoint(new Vector3(targetWorldPos.x, targetWorldPos.y, 0f));
                Shader.SetGlobalVector(WakeCenterPositionId, new Vector4(viewportPos.x, viewportPos.y, 0, 0));
            }

            Shader.SetGlobalFloat(WakeOrthoSizeId, Camera.orthographicSize);
        }

        private void HandleDebugControls()
        {
            // F1 - Shake
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Shake(debugShakeIntensity);
            }

            // F2 - Punch (random direction)
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                Punch(dir, debugPunchForce);
            }

            // F3 - Chromatic Aberration
            if (Input.GetKeyDown(KeyCode.F3))
            {
                SetChromaticAberration(debugChromaticIntensity, 0.3f);
            }

            // F4 - Vignette
            if (Input.GetKeyDown(KeyCode.F4))
            {
                SetVignette(debugVignetteIntensity, 0.5f);
            }

            // F5 - Flash
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Flash(Color.white, 0.1f);
            }

            // F6 - Reset all effects
            if (Input.GetKeyDown(KeyCode.F6))
            {
                ResetEffects();
            }
        }

        private Vector2 CalculateTargetPosition(bool useInterpolatedPosition, float speed)
        {
            Vector2 basePos = useInterpolatedPosition
                ? _targetProvider.GetInterpolatedPosition()
                : _targetProvider.GetTargetPosition();

            // Velocity-based look-ahead
            Vector2 velocity = _targetProvider.GetTargetVelocity();
            Vector2 targetVelocityLookAhead = Vector2.zero;

            if (velocity.sqrMagnitude > 0.1f && CurrentPreset.VelocityLookAheadDistance > 0)
            {
                // Distance-based (no cap, scales linearly with speed)
                float speedFactor = speed / CurrentPreset.SpeedZoomMaxSpeed;
                float lookAheadDistance = CurrentPreset.VelocityLookAheadDistance * speedFactor;

                // Time-based prediction (where ship will be in X seconds)
                float predictionLookAhead = speed * CurrentPreset.VelocityLookAheadPredictionTime;

                // Use the larger of the two approaches
                float totalLookAhead = Mathf.Max(lookAheadDistance, predictionLookAhead);

                // Compensate for zoom (keeps screen-space offset consistent)
                if (CurrentPreset.VelocityLookAheadScaleWithZoom && CurrentPreset.DefaultZoom > 0)
                {
                    float zoomFactor = _currentZoom / CurrentPreset.DefaultZoom;
                    totalLookAhead *= zoomFactor;
                }

                targetVelocityLookAhead = velocity.normalized * totalLookAhead;
            }

            _velocityLookAhead = Vector2.SmoothDamp(
                _velocityLookAhead,
                targetVelocityLookAhead,
                ref _velocityLookAheadVelocity,
                CurrentPreset.VelocityLookAheadSmoothing
            );

            // Focus-based look-ahead (aim/cursor direction)
            Vector2 targetFocusLookAhead = Vector2.zero;

            if (_targetProvider.HasFocus && CurrentPreset.FocusLookAheadDistance > 0)
            {
                Vector2 focusDir = _targetProvider.GetFocusDirection();
                if (focusDir.sqrMagnitude > 0.1f)
                {
                    // Apply speed-based ramp curve to focus look-ahead
                    float normalizedSpeed = Mathf.Clamp01(speed / CurrentPreset.SpeedZoomMaxSpeed);
                    float curveMultiplier = CurrentPreset.FocusLookAheadSpeedCurve.Evaluate(normalizedSpeed);
                    float focusLookAhead = CurrentPreset.FocusLookAheadDistance * curveMultiplier;

                    // Also scale focus look-ahead with zoom for consistency
                    if (CurrentPreset.VelocityLookAheadScaleWithZoom && CurrentPreset.DefaultZoom > 0)
                    {
                        float zoomFactor = _currentZoom / CurrentPreset.DefaultZoom;
                        focusLookAhead *= zoomFactor;
                    }

                    targetFocusLookAhead = focusDir * focusLookAhead;
                }
            }

            _focusLookAhead = Vector2.SmoothDamp(
                _focusLookAhead,
                targetFocusLookAhead,
                ref _focusLookAheadVelocity,
                CurrentPreset.FocusLookAheadSmoothing
            );

            // Clamp combined look-ahead if max distance is set
            Vector2 combinedLookAhead = _velocityLookAhead + _focusLookAhead;
            if (CurrentPreset.MaxLookAheadDistance > 0 && combinedLookAhead.magnitude > CurrentPreset.MaxLookAheadDistance)
            {
                combinedLookAhead = combinedLookAhead.normalized * CurrentPreset.MaxLookAheadDistance;
            }

            return basePos + combinedLookAhead;
        }

        private float GetAdaptiveFollowDampening(float speed)
        {
            float speedNormalized = Mathf.Clamp01(speed / CurrentPreset.SpeedZoomMaxSpeed);
            return Mathf.Lerp(
                CurrentPreset.FollowDampeningMax,
                CurrentPreset.FollowDampeningMin,
                speedNormalized
            );
        }

        private Vector2 ApplyScreenBoundsConstraint(Vector2 desiredCameraPos, Vector2 targetPos)
        {
            if (!CurrentPreset.EnableScreenBounds) return desiredCameraPos;

            // Calculate viewport dimensions based on current zoom
            float halfHeight = _currentZoom;
            float halfWidth = halfHeight * Camera.aspect;

            // Target position relative to desired camera position
            Vector2 relativePos = targetPos - desiredCameraPos;

            // Convert to viewport space (0-1)
            float viewportX = (relativePos.x / halfWidth + 1f) * 0.5f;
            float viewportY = (relativePos.y / halfHeight + 1f) * 0.5f;

            // Define bounds based on margin
            float margin = CurrentPreset.ScreenBoundsMargin;
            float minBound = margin;
            float maxBound = 1f - margin;

            // Calculate required camera offset to keep target in bounds
            Vector2 offset = Vector2.zero;

            if (viewportX < minBound)
            {
                float overshoot = (minBound - viewportX) * halfWidth * 2f;
                offset.x = -overshoot;
            }
            else if (viewportX > maxBound)
            {
                float overshoot = (viewportX - maxBound) * halfWidth * 2f;
                offset.x = overshoot;
            }

            if (viewportY < minBound)
            {
                float overshoot = (minBound - viewportY) * halfHeight * 2f;
                offset.y = -overshoot;
            }
            else if (viewportY > maxBound)
            {
                float overshoot = (viewportY - maxBound) * halfHeight * 2f;
                offset.y = overshoot;
            }

            return desiredCameraPos + offset;
        }

        private void UpdatePresetTransition(float deltaTime)
        {
            if (!_isTransitioningPreset) return;

            _presetTransitionElapsed += deltaTime;
            float t = Mathf.Clamp01(_presetTransitionElapsed / _presetTransitionDuration);
            t = Mathf.SmoothStep(0, 1, t);

            CurrentPreset.LerpTo(_transitionTargetPreset, t);

            if (t >= 1f)
            {
                _isTransitioningPreset = false;
                _transitionTargetPreset = null;
            }
        }

        private bool IsPhysicsTarget()
        {
            if (!_targetProvider.HasTargets) return false;
            return _targetProvider.GetPrimaryTargetType() == CameraTargetType.PhysicsBody;
        }

        #region Public API - Target Management

        public void SetTarget(ICameraTarget target)
        {
            _targetProvider = new SingleTargetProvider();
            _targetProvider.SetPrimaryTarget(target);
            OnTargetChanged?.Invoke(target);
        }

        public void SetTargets(IEnumerable<ICameraTarget> targets, ICameraTarget primary = null)
        {
            var multiProvider = new MultiTargetProvider
            {
                Padding = CurrentPreset.MultiTargetPadding,
                MinZoom = CurrentPreset.MultiTargetMinZoom,
                MaxZoom = CurrentPreset.MultiTargetMaxZoom
            };

            foreach (var target in targets)
            {
                multiProvider.AddTarget(target);
            }

            if (primary != null)
            {
                multiProvider.SetPrimaryTarget(primary);
            }

            _targetProvider = multiProvider;
        }

        public void AddTarget(ICameraTarget target)
        {
            if (_targetProvider is SingleTargetProvider single)
            {
                var multi = new MultiTargetProvider
                {
                    Padding = CurrentPreset.MultiTargetPadding,
                    MinZoom = CurrentPreset.MultiTargetMinZoom,
                    MaxZoom = CurrentPreset.MultiTargetMaxZoom
                };

                var currentPos = single.GetTargetPosition();
                if (currentPos != Vector2.zero)
                {
                    multi.AddTarget(new PositionTarget(currentPos));
                }

                multi.AddTarget(target);
                _targetProvider = multi;
            }
            else
            {
                _targetProvider.AddTarget(target);
            }
        }

        public void RemoveTarget(ICameraTarget target)
        {
            _targetProvider.RemoveTarget(target);
        }

        public void ClearTargets()
        {
            _targetProvider.ClearTargets();
        }

        #endregion

        #region Public API - Preset Management

        public void ApplyPreset(CameraPreset preset, bool immediate = false, float transitionTime = 0.5f)
        {
            if (preset == null) return;

            var newInstance = preset.CreateInstance();

            if (immediate)
            {
                CurrentPreset = newInstance;
                _zoomController = new SpeedBasedZoom(CurrentPreset);
                _effectsManager.SetPreset(CurrentPreset);
                _isTransitioningPreset = false;
            }
            else
            {
                _transitionTargetPreset = newInstance;
                _presetTransitionDuration = transitionTime;
                _presetTransitionElapsed = 0f;
                _isTransitioningPreset = true;
            }

            OnPresetChanged?.Invoke(preset);
        }

        #endregion

        #region Public API - Update Mode

        public void SetUpdateMode(CameraUpdateMode mode)
        {
            updateMode = mode;
        }

        public void SetUseInterpolation(bool enabled)
        {
            useInterpolation = enabled;
        }

        #endregion

        #region Public API - Zoom

        public void SetZoom(float zoom, float transitionTime = 0.5f)
        {
            _zoomController.SetUserZoom(zoom, transitionTime);
        }

        public void ResetZoom(float transitionTime = 0.5f)
        {
            _zoomController.ResetUserZoom(transitionTime);
        }

        public void ScrollZoom(float delta)
        {
            _zoomController.ScrollZoom(delta);
        }

        public float GetCurrentZoom() => _currentZoom;
        public float GetUserZoomLevel() => _zoomController.UserZoomLevel;
        public float GetSpeedZoomDelta() => _zoomController.SpeedZoomDelta;

        #endregion

        #region Public API - Effects

        public void Shake(float trauma)
        {
            _effectsManager.AddTrauma(trauma);
        }

        public void Punch(Vector2 direction, float force, float duration = 0.15f)
        {
            _effectsManager.Punch(direction, force, duration);
        }

        public void Flash(Color color, float duration)
        {
            _effectsManager.Flash(color, duration);
        }

        public void SetChromaticAberration(float intensity, float duration = 0f)
        {
            _effectsManager.SetChromaticAberration(intensity, duration);
        }

        public void SetVignette(float intensity, float duration = 0f)
        {
            _effectsManager.SetVignette(intensity, duration);
        }

        public void SetLensDistortion(float intensity, float duration = 0f)
        {
            _effectsManager.SetLensDistortion(intensity, duration);
        }

        public void ResetEffects()
        {
            _effectsManager.ResetAll();
        }

        #endregion

        #region Public API - Utility

        public void SnapToTarget()
        {
            if (!_targetProvider.HasTargets) return;

            _currentPosition = _targetProvider.GetTargetPosition();
            _positionVelocity = Vector2.zero;
            _velocityLookAhead = Vector2.zero;
            _velocityLookAheadVelocity = Vector2.zero;
            _focusLookAhead = Vector2.zero;
            _focusLookAheadVelocity = Vector2.zero;

            transform.position = new Vector3(_currentPosition.x, _currentPosition.y, transform.position.z);
        }

        public Vector2 GetCurrentPosition() => _currentPosition;

        public ICameraTargetProvider GetTargetProvider() => _targetProvider;

        #endregion

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_currentPosition, 0.5f);

            if (_targetProvider != null && _targetProvider.HasTargets)
            {
                Gizmos.color = Color.green;
                Vector2 targetPos = _targetProvider.GetTargetPosition();
                Gizmos.DrawWireSphere(targetPos, 0.3f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_currentPosition, targetPos);

                // Velocity look-ahead (magenta)
                if (_velocityLookAhead.sqrMagnitude > 0.01f)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(targetPos, targetPos + _velocityLookAhead);
                }

                // Focus look-ahead (blue)
                if (_focusLookAhead.sqrMagnitude > 0.01f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(targetPos, targetPos + _focusLookAhead);
                }
            }
        }

        private class PositionTarget : ICameraTarget
        {
            private readonly Vector2 _position;

            public PositionTarget(Vector2 position)
            {
                _position = position;
            }

            public Vector2 Position => _position;
            public Vector2 InterpolatedPosition => _position;
            public Vector2 Velocity => Vector2.zero;
            public Vector2 FocusDirection => Vector2.zero;
            public bool HasFocus => false;
            public bool IsValid => true;
            public float Priority => 1f;
            public CameraTargetType TargetType => CameraTargetType.Transform;
        }
    }
}