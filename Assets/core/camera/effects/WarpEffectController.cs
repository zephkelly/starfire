using System;
using Starfire.Core.Cam.Targeting;
using UnityEngine;

namespace Starfire.Core.Cam.Effects
{
    public enum WarpState
    {
        Idle,
        WarpingIn,
        InWarp,
        WarpingOut
    }

    /// <summary>
    /// Controls the warp visual effect, orchestrating star streaking, chromatic aberration,
    /// lens distortion, and camera zoom effects.
    /// </summary>
    public class WarpEffectController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private WarpEffectConfig config;

        [Header("References")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private bool autoFindCamera = true;

        [Header("Speed-Based Mode")]
        [SerializeField] private bool useSpeedBasedWarp = true;
        [Tooltip("Optional direct reference to a Rigidbody2D to track. If null, uses camera target.")]
        [SerializeField] private Rigidbody2D trackedRigidbody;

        [Header("Post Processing")]
        [Tooltip("Use custom directional chromatic aberration instead of URP's radial effect. Requires DirectionalChromaticAberrationFeature on the renderer.")]
        [SerializeField] private bool useDirectionalChromatic = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugControls;
        [SerializeField] private KeyCode debugWarpKey = KeyCode.LeftShift;
        [SerializeField] private bool enableConsoleLogging;

        // State
        public WarpState CurrentState { get; private set; } = WarpState.Idle;
        public float WarpIntensity { get; private set; }
        public bool IsWarping => CurrentState != WarpState.Idle;

        // Events
        public event Action OnWarpStarted;
        public event Action OnWarpEnded;
        public event Action<float> OnWarpIntensityChanged;

        // Shader property IDs
        private static readonly int WarpIntensityId = Shader.PropertyToID("_WarpIntensity");
        private static readonly int WarpStretchId = Shader.PropertyToID("_WarpStretch");
        private static readonly int WarpDirectionId = Shader.PropertyToID("_WarpDirection");
        private static readonly int WarpBrightnessBoostId = Shader.PropertyToID("_WarpBrightnessBoost");
        private static readonly int WarpNebulaStretchId = Shader.PropertyToID("_WarpNebulaStretch");
        private static readonly int WarpNebulaFadeId = Shader.PropertyToID("_WarpNebulaFade");

        // Streak shape shader property IDs
        private static readonly int WarpStreakWidthId = Shader.PropertyToID("_WarpStreakWidth");
        private static readonly int WarpEdgeSoftnessMinId = Shader.PropertyToID("_WarpEdgeSoftnessMin");
        private static readonly int WarpEdgeSoftnessMaxId = Shader.PropertyToID("_WarpEdgeSoftnessMax");
        private static readonly int WarpLeadingEdgeRatioId = Shader.PropertyToID("_WarpLeadingEdgeRatio");
        private static readonly int WarpTrailingFadeStartId = Shader.PropertyToID("_WarpTrailingFadeStart");
        private static readonly int WarpBlendTransitionId = Shader.PropertyToID("_WarpBlendTransition");
        private static readonly int WarpDistantMinEffectId = Shader.PropertyToID("_WarpDistantMinEffect");
        private static readonly int WarpParallaxMultiplierId = Shader.PropertyToID("_WarpParallaxMultiplier");
        private static readonly int WarpTailTaperPowerId = Shader.PropertyToID("_WarpTailTaperPower");

        // Wobble shader property IDs
        private static readonly int WobbleIntensityId = Shader.PropertyToID("_WobbleIntensity");
        private static readonly int WobbleFrequencyId = Shader.PropertyToID("_WobbleFrequency");
        private static readonly int WobbleSpeedId = Shader.PropertyToID("_WobbleSpeed");

        // Internal state
        private Vector2 _warpDirection = Vector2.up;
        private float _transitionDuration;
        private float _transitionElapsed;
        private float _transitionStartIntensity;
        private float _transitionTargetIntensity;

        private float _baseOrthoSize;
        private Camera _camera;

        // Speed-based mode state
        private float _targetIntensity;
        private float _intensityVelocity;

        // Debug state
        private float _lastLogTime;
        private WarpState _previousState = WarpState.Idle;
        private const float LogInterval = 1f;

        // Debug properties for WarpDebugPanel
        public Vector2 WarpDirection => _warpDirection;
        public Vector2 LastTrackedVelocity { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float TargetIntensityDebug => _targetIntensity;
        public bool UseSpeedBasedWarp => useSpeedBasedWarp;
        public WarpEffectConfig Config => config;
        public CameraController CameraControllerRef => cameraController;
        public Rigidbody2D TrackedRigidbodyRef => trackedRigidbody;

        public bool HasValidVelocitySource =>
            trackedRigidbody != null ||
            (cameraController?.GetTargetProvider()?.HasTargets ?? false);

        private void Awake()
        {
            if (autoFindCamera && cameraController == null)
            {
                cameraController = FindFirstObjectByType<CameraController>();
            }

            if (cameraController != null)
            {
                _camera = cameraController.Camera;
            }
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            if (_camera != null)
            {
                _baseOrthoSize = _camera.orthographicSize;
            }

            // Initialize shader globals to default state
            ResetShaderGlobals();
        }

        private void Update()
        {
            if (useSpeedBasedWarp)
            {
                UpdateSpeedBasedWarp();
            }
            else
            {
                UpdateTransition();
            }

            UpdateShaderGlobals();
            UpdatePostProcessing();
            UpdateZoom();

            if (enableDebugControls && !useSpeedBasedWarp)
            {
                HandleDebugControls();
            }
        }

        private void HandleDebugControls()
        {
            if (Input.GetKeyDown(debugWarpKey))
            {
                StartWarp();
            }
            if (Input.GetKeyUp(debugWarpKey))
            {
                StopWarp();
            }
        }

        private void UpdateTransition()
        {
            if (CurrentState == WarpState.Idle || CurrentState == WarpState.InWarp)
                return;

            _transitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_transitionElapsed / _transitionDuration);

            // Apply easing curve if config is available
            if (config != null)
            {
                t = config.transitionCurve.Evaluate(t);
            }
            else
            {
                t = Mathf.SmoothStep(0f, 1f, t);
            }

            float newIntensity = Mathf.Lerp(_transitionStartIntensity, _transitionTargetIntensity, t);
            SetIntensityInternal(newIntensity);

            if (_transitionElapsed >= _transitionDuration)
            {
                CompleteTransition();
            }
        }

        private void CompleteTransition()
        {
            SetIntensityInternal(_transitionTargetIntensity);

            if (CurrentState == WarpState.WarpingIn)
            {
                CurrentState = WarpState.InWarp;
            }
            else if (CurrentState == WarpState.WarpingOut)
            {
                CurrentState = WarpState.Idle;
                OnWarpEnded?.Invoke();
            }
        }

        private void SetIntensityInternal(float intensity)
        {
            if (Mathf.Approximately(WarpIntensity, intensity)) return;

            WarpIntensity = intensity;
            OnWarpIntensityChanged?.Invoke(WarpIntensity);
        }

        private void UpdateShaderGlobals()
        {
            float stretch = config != null ? config.GetStretchMultiplier(WarpIntensity) : Mathf.Lerp(1f, 20f, WarpIntensity);
            float brightnessMult = config != null ? config.GetBrightnessMultiplier(WarpIntensity) : Mathf.Lerp(1f, 1.0f, WarpIntensity);
            float nebulaStretch = config != null ? config.GetNebulaStretchMultiplier(WarpIntensity) : Mathf.Lerp(1f, 1f + (stretch - 1f) * 0.3f, WarpIntensity);
            float nebulaFade = config != null ? config.GetNebulaFade(WarpIntensity) : WarpIntensity * 0.3f;

            // Core warp globals
            Shader.SetGlobalFloat(WarpIntensityId, WarpIntensity);
            Shader.SetGlobalFloat(WarpStretchId, stretch);
            Shader.SetGlobalVector(WarpDirectionId, new Vector4(_warpDirection.x, _warpDirection.y, 0f, 0f));
            Shader.SetGlobalFloat(WarpBrightnessBoostId, brightnessMult);
            Shader.SetGlobalFloat(WarpNebulaStretchId, nebulaStretch);
            Shader.SetGlobalFloat(WarpNebulaFadeId, nebulaFade);

            // Streak shape globals
            Shader.SetGlobalFloat(WarpStreakWidthId, config != null ? config.streakWidthAtFullWarp : 0.2f);
            Shader.SetGlobalFloat(WarpEdgeSoftnessMinId, config != null ? config.edgeSoftnessMin : 0.3f);
            Shader.SetGlobalFloat(WarpEdgeSoftnessMaxId, config != null ? config.edgeSoftnessMax : 0.05f);
            Shader.SetGlobalFloat(WarpLeadingEdgeRatioId, config != null ? config.leadingEdgeRatio : 0.15f);
            Shader.SetGlobalFloat(WarpTrailingFadeStartId, config != null ? config.trailingFadeStart : 0.3f);
            Shader.SetGlobalFloat(WarpBlendTransitionId, config != null ? config.blendTransitionRange : 0.15f);
            Shader.SetGlobalFloat(WarpDistantMinEffectId, config != null ? config.distantLayerMinEffect : 0.3f);
            Shader.SetGlobalFloat(WarpParallaxMultiplierId, config != null ? config.parallaxDepthMultiplier : 10f);
            Shader.SetGlobalFloat(WarpTailTaperPowerId, config != null ? config.tailTaperPower : 0.7f);

            // Wobble globals
            Shader.SetGlobalFloat(WobbleIntensityId, config != null ? config.wobbleIntensity : 0f);
            Shader.SetGlobalFloat(WobbleFrequencyId, config != null ? config.wobbleFrequency : 5f);
            Shader.SetGlobalFloat(WobbleSpeedId, config != null ? config.wobbleSpeed : 2f);
        }

        private void UpdatePostProcessing()
        {
            if (cameraController == null) return;

            // Only use URP's built-in chromatic aberration if not using directional version
            // The directional version reads _WarpIntensity global directly from shader
            if (!useDirectionalChromatic)
            {
                float chromatic = config != null ? config.GetChromaticAberration(WarpIntensity) : WarpIntensity * 0.7f;
                cameraController.SetChromaticAberration(chromatic);
            }
            else
            {
                // Ensure built-in chromatic is disabled when using directional
                cameraController.SetChromaticAberration(0f);
            }

            float lensDistortion = config != null ? config.GetLensDistortion(WarpIntensity) : WarpIntensity * 0.15f;
            cameraController.SetLensDistortion(lensDistortion);
        }

        private void UpdateZoom()
        {
            if (_camera == null) return;

            float zoomMultiplier = config != null ? config.GetZoomMultiplier(WarpIntensity) : Mathf.Lerp(1f, 1.2f, WarpIntensity);
            _camera.orthographicSize = _baseOrthoSize * zoomMultiplier;
        }

        private void UpdateSpeedBasedWarp()
        {
            Vector2 velocity = GetTargetVelocity();
            float speed = velocity.magnitude;

            // Cache for debug
            LastTrackedVelocity = velocity;
            CurrentSpeed = speed;

            // Get thresholds from config or use defaults
            float minSpeed = config != null ? config.minSpeedForEffect : 5f;
            float maxSpeed = config != null ? config.maxSpeedForEffect : 50f;
            float smoothing = config != null ? config.intensitySmoothing : 0.15f;

            // Console logging warnings
            if (enableConsoleLogging)
            {
                LogWarningsIfNeeded(speed, minSpeed);
            }

            // Map speed to intensity (0-1)
            float normalizedSpeed = Mathf.InverseLerp(minSpeed, maxSpeed, speed);

            // Apply curve from config if available
            float curvedIntensity = config != null
                ? config.speedToIntensityCurve.Evaluate(normalizedSpeed)
                : Mathf.SmoothStep(0f, 1f, normalizedSpeed);

            _targetIntensity = curvedIntensity;

            // Smooth the intensity changes
            float smoothedIntensity = Mathf.SmoothDamp(WarpIntensity, _targetIntensity, ref _intensityVelocity, smoothing);
            SetIntensityInternal(smoothedIntensity);

            // Set direction from velocity (if moving fast enough)
            if (speed > 0.1f)
            {
                _warpDirection = velocity.normalized;
            }

            // Update state for event firing
            if (WarpIntensity > 0.01f && CurrentState == WarpState.Idle)
            {
                if (_camera != null)
                {
                    _baseOrthoSize = _camera.orthographicSize;
                }
                CurrentState = WarpState.InWarp;
                OnWarpStarted?.Invoke();
            }
            else if (WarpIntensity <= 0.01f && CurrentState != WarpState.Idle)
            {
                CurrentState = WarpState.Idle;
                OnWarpEnded?.Invoke();
            }

            // Log state transitions
            if (enableConsoleLogging && CurrentState != _previousState)
            {
                Debug.Log($"[Warp] State changed: {_previousState} → {CurrentState} (speed: {speed:F1})");
                _previousState = CurrentState;
            }

            // Throttled periodic logging
            if (enableConsoleLogging && Time.time - _lastLogTime >= LogInterval)
            {
                _lastLogTime = Time.time;
                Debug.Log($"[Warp] Speed: {speed:F1} | Intensity: {WarpIntensity:F2} | Dir: ({_warpDirection.x:F2}, {_warpDirection.y:F2}) | State: {CurrentState}");
            }
        }

        private void LogWarningsIfNeeded(float speed, float minSpeed)
        {
            if (!HasValidVelocitySource && Time.time - _lastLogTime >= LogInterval)
            {
                Debug.LogWarning("[Warp] WARNING: No velocity source found!");
            }
            else if (speed > 0.1f && speed < minSpeed && Time.time - _lastLogTime >= LogInterval)
            {
                Debug.LogWarning($"[Warp] WARNING: Speed ({speed:F1}) below threshold ({minSpeed:F1})");
            }
        }

        private Vector2 GetTargetVelocity()
        {
            // Option 1: Direct reference to a Rigidbody2D
            if (trackedRigidbody != null)
            {
                return trackedRigidbody.linearVelocity;
            }

            // Option 2: Get from CameraController's target provider
            if (cameraController != null)
            {
                var targetProvider = cameraController.GetTargetProvider();
                if (targetProvider != null && targetProvider.HasTargets)
                {
                    return targetProvider.GetTargetVelocity();
                }
            }

            return Vector2.zero;
        }

        private void ResetShaderGlobals()
        {
            // Core warp globals
            Shader.SetGlobalFloat(WarpIntensityId, 0f);
            Shader.SetGlobalFloat(WarpStretchId, 1f);
            Shader.SetGlobalVector(WarpDirectionId, new Vector4(0f, 1f, 0f, 0f));
            Shader.SetGlobalFloat(WarpBrightnessBoostId, 1f);
            Shader.SetGlobalFloat(WarpNebulaStretchId, 1f);
            Shader.SetGlobalFloat(WarpNebulaFadeId, 0f);

            // Streak shape globals (set to defaults)
            Shader.SetGlobalFloat(WarpStreakWidthId, config != null ? config.streakWidthAtFullWarp : 0.2f);
            Shader.SetGlobalFloat(WarpEdgeSoftnessMinId, config != null ? config.edgeSoftnessMin : 0.3f);
            Shader.SetGlobalFloat(WarpEdgeSoftnessMaxId, config != null ? config.edgeSoftnessMax : 0.05f);
            Shader.SetGlobalFloat(WarpLeadingEdgeRatioId, config != null ? config.leadingEdgeRatio : 0.15f);
            Shader.SetGlobalFloat(WarpTrailingFadeStartId, config != null ? config.trailingFadeStart : 0.3f);
            Shader.SetGlobalFloat(WarpBlendTransitionId, config != null ? config.blendTransitionRange : 0.15f);
            Shader.SetGlobalFloat(WarpDistantMinEffectId, config != null ? config.distantLayerMinEffect : 0.3f);
            Shader.SetGlobalFloat(WarpParallaxMultiplierId, config != null ? config.parallaxDepthMultiplier : 10f);
            Shader.SetGlobalFloat(WarpTailTaperPowerId, config != null ? config.tailTaperPower : 0.7f);

            // Wobble globals
            Shader.SetGlobalFloat(WobbleIntensityId, config != null ? config.wobbleIntensity : 0f);
            Shader.SetGlobalFloat(WobbleFrequencyId, config != null ? config.wobbleFrequency : 5f);
            Shader.SetGlobalFloat(WobbleSpeedId, config != null ? config.wobbleSpeed : 2f);
        }

        #region Public API

        /// <summary>
        /// Start the warp effect with optional custom duration.
        /// </summary>
        /// <param name="duration">Transition duration. Use -1 for default from config.</param>
        public void StartWarp(float duration = -1f)
        {
            if (CurrentState == WarpState.InWarp || CurrentState == WarpState.WarpingIn)
                return;

            // Capture base zoom when starting warp
            if (_camera != null && CurrentState == WarpState.Idle)
            {
                _baseOrthoSize = _camera.orthographicSize;
            }

            _transitionStartIntensity = WarpIntensity;
            _transitionTargetIntensity = 1f;
            _transitionDuration = duration > 0f ? duration : (config != null ? config.defaultWarpInDuration : 1.5f);
            _transitionElapsed = 0f;

            CurrentState = WarpState.WarpingIn;
            OnWarpStarted?.Invoke();
        }

        /// <summary>
        /// Stop the warp effect with optional custom duration.
        /// </summary>
        /// <param name="duration">Transition duration. Use -1 for default from config.</param>
        public void StopWarp(float duration = -1f)
        {
            if (CurrentState == WarpState.Idle || CurrentState == WarpState.WarpingOut)
                return;

            _transitionStartIntensity = WarpIntensity;
            _transitionTargetIntensity = 0f;
            _transitionDuration = duration > 0f ? duration : (config != null ? config.defaultWarpOutDuration : 1.0f);
            _transitionElapsed = 0f;

            CurrentState = WarpState.WarpingOut;
        }

        /// <summary>
        /// Set the warp direction (ship heading). Call each frame while warping.
        /// </summary>
        /// <param name="direction">Normalized direction vector.</param>
        public void SetWarpDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                _warpDirection = direction.normalized;
            }
        }

        /// <summary>
        /// Directly set warp intensity, bypassing the state machine.
        /// Useful for manual control or previewing effects.
        /// </summary>
        /// <param name="intensity">Warp intensity from 0 to 1.</param>
        public void SetWarpIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            SetIntensityInternal(intensity);

            // Update state based on intensity
            if (intensity > 0f && CurrentState == WarpState.Idle)
            {
                if (_camera != null)
                {
                    _baseOrthoSize = _camera.orthographicSize;
                }
                CurrentState = WarpState.InWarp;
                OnWarpStarted?.Invoke();
            }
            else if (intensity <= 0f && CurrentState != WarpState.Idle)
            {
                CurrentState = WarpState.Idle;
                OnWarpEnded?.Invoke();
            }
        }

        /// <summary>
        /// Force reset all warp effects immediately.
        /// </summary>
        public void ResetWarp()
        {
            CurrentState = WarpState.Idle;
            WarpIntensity = 0f;
            _transitionElapsed = 0f;

            ResetShaderGlobals();

            if (cameraController != null)
            {
                cameraController.SetChromaticAberration(0f);
                cameraController.SetLensDistortion(0f);
            }

            if (_camera != null)
            {
                _camera.orthographicSize = _baseOrthoSize;
            }

            OnWarpEnded?.Invoke();
        }

        #endregion
    }
}
