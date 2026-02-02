using System;
using System.Collections.Generic;
using Starfire.Core.Cam;
using Starfire.Core.Cam.Effects;
using Starfire.Core.Cam.Targeting;
using Starfire.Core.Cam.Zoom;
using Starfire.Core.V2.Cam.Config;
using Starfire.Core.V2.Cam.Following;
using UnityEngine;
using UnityEngine.Rendering;

namespace Starfire.Core.V2.Cam
{
    public enum CameraUpdateMode
    {
        LateUpdate,
        FixedUpdate,
        Auto
    }

    [RequireComponent(typeof(UnityEngine.Camera))]
    public class VelocityCameraController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VelocityCameraPreset defaultPreset;

        [Header("Initial Target")]
        [SerializeField] private EntityCameraTarget initialTarget;
        [SerializeField] private bool autoFindTarget = true;
        [SerializeField] private bool snapOnStart = true;

        [Header("Update Mode")]
        [SerializeField] private CameraUpdateMode updateMode = CameraUpdateMode.Auto;

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
        public VelocityCameraPresetInstance CurrentPreset { get; private set; }
        public IFollowStrategy Follower { get; private set; }
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
        private V2EffectsManager _effectsManager;

        private float _currentZoom;
        private float _baseRotation;

        private bool _isTransitioningPreset;
        private VelocityCameraPresetInstance _transitionTargetPreset;
        private float _presetTransitionDuration;
        private float _presetTransitionElapsed;

        private bool _updatedThisFrame;

        public event Action<VelocityCameraPreset> OnPresetChanged;
        public event Action<ICameraTarget> OnTargetChanged;

        private void Awake()
        {
            Camera = GetComponent<UnityEngine.Camera>();
            _targetProvider = new SingleTargetProvider();

            Volume volume = postProcessVolume;
            if (volume == null && autoFindVolume)
            {
                volume = FindFirstObjectByType<Volume>();
            }

            PostProcessingBridge postProcessing = null;
            if (volume != null)
            {
                postProcessing = new PostProcessingBridge(volume);
            }

            if (defaultPreset != null)
            {
                ApplyPreset(defaultPreset, immediate: true);
            }
            else
            {
                CreateDefaultPreset();
            }

            _effectsManager = new V2EffectsManager(CurrentPreset, postProcessing);

            Vector2 initialPosition = transform.position;
            Follower = new VelocityTrackingFollower(CurrentPreset);
            Follower.Initialize(initialPosition);

            _currentZoom = Camera.orthographicSize;
            _baseRotation = transform.eulerAngles.z;

#if UNITY_EDITOR
            VelocityCameraPreset.OnPresetModified += OnPresetAssetModified;
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
            VelocityCameraPreset.OnPresetModified -= OnPresetAssetModified;
        }

        private void OnPresetAssetModified(VelocityCameraPreset preset)
        {
            if (CurrentPreset != null && CurrentPreset.Source == preset)
            {
                CurrentPreset.RefreshFromSource();
                Follower?.SetPreset(CurrentPreset);
                _effectsManager?.SetPreset(CurrentPreset);
            }
        }
#endif

        private void CreateDefaultPreset()
        {
            CurrentPreset = new VelocityCameraPresetInstance(null)
            {
                VelocityTrackingThreshold = 5f,
                VelocityTrackingFullSpeed = 15f,
                PositionCorrectionTime = 0.1f,
                MaxPositionError = 2f,
                VelocityTrackingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                LowSpeedDampening = 0.15f,
                FocusLookAheadDistance = 3f,
                FocusSmoothing = 0.2f,
                ScaleFocusWithZoom = true,
                FocusSpeedInfluence = AnimationCurve.Linear(0, 1, 1, 0.3f),
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

            _zoomController = new V2ZoomController(CurrentPreset);
        }

        private void Update()
        {
            _updatedThisFrame = false;

            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                _zoomController.ScrollZoom(scrollDelta);
            }

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
                UpdateCamera(Time.fixedDeltaTime);
                _updatedThisFrame = true;
            }
        }

        private void LateUpdate()
        {
            if (_updatedThisFrame) return;

            var effectiveMode = EffectiveUpdateMode;
            if (effectiveMode == CameraUpdateMode.LateUpdate)
            {
                UpdateCamera(Time.deltaTime);
            }
        }

        private void UpdateCamera(float deltaTime)
        {
            UpdatePresetTransition(deltaTime);

            if (!_targetProvider.HasTargets)
            {
                _effectsManager.Update(deltaTime);
                return;
            }

            Vector2 targetPos = _targetProvider.GetInterpolatedPosition();
            Vector2 targetVel = _targetProvider.GetTargetVelocity();
            Vector2 focusDir = _targetProvider.HasFocus
                ? _targetProvider.GetFocusDirection()
                : Vector2.zero;
            float speed = targetVel.magnitude;

            // Update zoom
            float recommendedZoom = _targetProvider.GetRecommendedZoom();
            _zoomController.Update(speed, recommendedZoom, deltaTime);
            _currentZoom = _zoomController.CurrentZoom;

            // Update follower
            Vector2 newPosition = Follower.Update(
                targetPos,
                targetVel,
                focusDir,
                _currentZoom,
                deltaTime
            );

            // Apply screen bounds constraint
            newPosition = ApplyScreenBoundsConstraint(newPosition, targetPos);

            // Update effects
            _effectsManager.Update(deltaTime);

            // Apply final position with effects
            Vector3 finalPos = new Vector3(newPosition.x, newPosition.y, transform.position.z);
            float finalRotation = _baseRotation;
            float finalZoom = _currentZoom;

            _effectsManager.ApplyAllEffects(ref finalPos, ref finalRotation, ref finalZoom);

            transform.position = finalPos;
            transform.rotation = Quaternion.Euler(0, 0, finalRotation);
            Camera.orthographicSize = finalZoom;
        }

        private void HandleDebugControls()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Shake(debugShakeIntensity);
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                Punch(dir, debugPunchForce);
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                SetChromaticAberration(debugChromaticIntensity, 0.3f);
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                SetVignette(debugVignetteIntensity, 0.5f);
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                Flash(Color.white, 0.1f);
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                ResetEffects();
            }
        }

        private Vector2 ApplyScreenBoundsConstraint(Vector2 desiredCameraPos, Vector2 targetPos)
        {
            if (!CurrentPreset.EnableScreenBounds) return desiredCameraPos;

            float halfHeight = _currentZoom;
            float halfWidth = halfHeight * Camera.aspect;

            Vector2 relativePos = targetPos - desiredCameraPos;

            float viewportX = (relativePos.x / halfWidth + 1f) * 0.5f;
            float viewportY = (relativePos.y / halfHeight + 1f) * 0.5f;

            float margin = CurrentPreset.ScreenBoundsMargin;
            float minBound = margin;
            float maxBound = 1f - margin;

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

        public void ApplyPreset(VelocityCameraPreset preset, bool immediate = false, float transitionTime = 0.5f)
        {
            if (preset == null) return;

            var newInstance = preset.CreateInstance();

            if (immediate)
            {
                CurrentPreset = newInstance;
                _zoomController = new V2ZoomController(CurrentPreset);
                Follower?.SetPreset(CurrentPreset);
                _effectsManager?.SetPreset(CurrentPreset);
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

        public void ResetEffects()
        {
            _effectsManager.ResetAll();
        }

        #endregion

        #region Public API - Utility

        public void SnapToTarget()
        {
            if (!_targetProvider.HasTargets) return;

            Vector2 targetPos = _targetProvider.GetTargetPosition();
            Follower.SnapTo(targetPos);

            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        }

        public Vector2 GetCurrentPosition() => Follower.CurrentPosition;

        public ICameraTargetProvider GetTargetProvider() => _targetProvider;

        public float GetTrackingFactor() => Follower.TrackingFactor;

        public Vector2 GetPositionError() => Follower.PositionError;

        public Vector2 GetFocusOffset() => Follower.FocusOffset;

        /// <summary>
        /// Called when a floating origin shift occurs. Updates internal tracking state.
        /// </summary>
        public void OnOriginShift(Vector2 shiftAmount)
        {
            Follower?.ShiftPosition(shiftAmount);
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            if (Follower == null) return;

            Vector2 currentPos = Follower.CurrentPosition;

            // Camera position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentPos, 0.5f);

            if (_targetProvider != null && _targetProvider.HasTargets)
            {
                Vector2 targetPos = _targetProvider.GetTargetPosition();

                // Target position
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetPos, 0.3f);

                // Line to target
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(currentPos, targetPos);

                // Focus offset
                Vector2 focusOffset = Follower.FocusOffset;
                if (focusOffset.sqrMagnitude > 0.01f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(targetPos, targetPos + focusOffset);
                    Gizmos.DrawWireSphere(targetPos + focusOffset, 0.15f);
                }

                // Position error
                Vector2 posError = Follower.PositionError;
                if (posError.sqrMagnitude > 0.01f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(currentPos, currentPos + posError);
                }

                // Tracking factor indicator (color based on mode)
                float trackingFactor = Follower.TrackingFactor;
                if (trackingFactor < 0.01f)
                {
                    Gizmos.color = Color.green; // Low speed mode
                }
                else if (trackingFactor > 0.99f)
                {
                    Gizmos.color = Color.red; // Velocity tracking mode
                }
                else
                {
                    Gizmos.color = Color.yellow; // Blend mode
                }
                Gizmos.DrawWireSphere(currentPos, 0.3f + trackingFactor * 0.3f);
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
            public Vector2 FocusDirectionRaw => Vector2.zero;
            public bool HasFocus => false;
            public bool IsWorldSpaceAim => false;
            public float AimMaxRadius => 0f;
            public bool IsValid => true;
            public float Priority => 1f;
            public CameraTargetType TargetType => CameraTargetType.Transform;
        }
    }
}
