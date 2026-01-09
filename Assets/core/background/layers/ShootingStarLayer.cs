using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Noise;
using Starfire.Core.Background.Behaviors;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// A layer that spawns and renders shooting stars.
    /// Stars originate outside the camera view and travel until they exit.
    /// Direction is influenced by Perlin noise for regional variation.
    /// Supports multiple behavior types with weighted UnityEngine.Random selection.
    /// </summary>
    [System.Serializable]
    public class ShootingStarLayer : StarfieldLayer
    {
        private const int MAX_STARS = 264;  // Must match shader array sizes

        [Header("Spawning")]
        [Tooltip("Average seconds between spawn attempts")]
        public float spawnInterval = 3f;

        [Tooltip("UnityEngine.Random variance in spawn timing")]
        public float spawnIntervalVariance = 2f;

        [Tooltip("Enable to limit the number of simultaneous shooting stars")]
        public bool limitActiveStars = true;

        [Tooltip("Maximum simultaneous shooting stars (only used when limitActiveStars is enabled)")]
        [Range(1, 264)]
        public int maxActiveStars = 5;

        [Header("Movement")]
        [Tooltip("Minimum visual speed in shader units per second")]
        [Min(0.01f)]
        public float speedMin = 8.00f;

        [Tooltip("Maximum visual speed in shader units per second")]
        [Min(0.01f)]
        public float speedMax = 22.00f;

        [Tooltip("Speed distribution curve (X=random 0-1, Y=lerp factor between min/max). Linear = uniform, ease-in = more slow stars, ease-out = more fast stars")]
        public AnimationCurve speedDistribution = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Visual trail length as fraction of screen height (0.15 = 15%)")]
        public float trailLength = 0.15f;

        [Tooltip("Random trail length variance")]
        public float trailLengthVariance = 0.05f;

        [Header("Direction Noise")]
        [Tooltip("Perlin noise scale (smaller = larger regions with same direction)")]
        public float noiseScale = 0.05f;

        [Tooltip("How much noise affects direction (0-1)")]
        [Range(0f, 1f)]
        public float noiseInfluence = 0.5f;

        [Tooltip("Random angle variance applied to the toward-viewport direction")]
        public float angleVariance = 30f;

        [Header("Appearance")]
        public Color starColor = Color.white;

        [Tooltip("Base brightness of the shooting star")]
        [Range(0.2f, 5f)]
        public float brightness = 1.5f;

        [Tooltip("UnityEngine.Random brightness variance (0 = all same brightness)")]
        [Range(0f, 1f)]
        public float brightnessVariance = 0.3f;

        [Tooltip("Exponent for speed-to-brightness curve (1=linear, <1=brighter slow stars, >1=dimmer slow stars)")]
        [Range(0.5f, 3f)]
        public float speedBrightnessCurve = 1f;

        [Tooltip("Minimum width as fraction of screen height (0.01 = 1%)")]
        [Min(0.001f)]
        public float starWidthMin = 0.01f;

        [Tooltip("Maximum width as fraction of screen height (0.03 = 3%)")]
        [Min(0.001f)]
        public float starWidthMax = 0.03f;

        [Header("Behavior Configuration")]
        [Tooltip("List of behavior configs with selection weights. Leave empty to use default behavior.")]
        [SerializeField] private List<ShootingStarBehaviorConfig> behaviorConfigs = new List<ShootingStarBehaviorConfig>();

        [Tooltip("Lifetime multiplier for Persistent behavior stars")]
        [Min(1f)]
        [SerializeField] private float persistentLifetimeMultiplier = 3f;

        [Header("Debug")]
        [Tooltip("Draw gizmos in Scene view showing star spawn points, paths, and destinations")]
        [SerializeField] private bool showSceneGizmos = false;

        [Tooltip("Color for spawn point markers")]
        [SerializeField] private Color gizmoSpawnColor = Color.green;

        [Tooltip("Color for current position markers")]
        [SerializeField] private Color gizmoCurrentColor = Color.yellow;

        [Tooltip("Color for destination markers")]
        [SerializeField] private Color gizmoDestinationColor = Color.red;

        // Runtime state
        [System.NonSerialized] private List<ShootingStarData> _activeStars = new List<ShootingStarData>();
        [System.NonSerialized] private float _nextSpawnTime;
        [System.NonSerialized] private Camera _camera;

        // Event mode state
        [System.NonSerialized] private EventModeConfig _activeEventMode;

        // Shader property IDs
        private static readonly int ActiveStarCountID = Shader.PropertyToID("_ActiveStarCount");
        private static readonly int StarPositionsID = Shader.PropertyToID("_StarPositions");
        private static readonly int StarParamsID = Shader.PropertyToID("_StarParams");
        private static readonly int StarColorID = Shader.PropertyToID("_StarColor");
        private static readonly int BrightnessID = Shader.PropertyToID("_Brightness");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");
        private static readonly int CameraOrthoSizeID = Shader.PropertyToID("_CameraOrthoSize");
        private static readonly int ReferenceZoomID = Shader.PropertyToID("_ReferenceZoom");

        // Arrays for passing to shader
        private Vector4[] _positionArray = new Vector4[MAX_STARS];
        private Vector4[] _paramsArray = new Vector4[MAX_STARS];

        public override Shader GetShader()
        {
            return Shader.Find("Starfire/ShootingStars");
        }

        public override void Initialize(Transform parent, Mesh quadMesh, int sortOrder)
        {
            base.Initialize(parent, quadMesh, sortOrder);

            _activeStars = new List<ShootingStarData>();
            _nextSpawnTime = Time.time + UnityEngine.Random.Range(0f, spawnInterval);
            _camera = Camera.main;

            // Ensure arrays are properly sized (may be stale from serialization)
            _positionArray = new Vector4[MAX_STARS];
            _paramsArray = new Vector4[MAX_STARS];
        }

        public override void Update()
        {
            if (_material == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            // Get effective values (may be overridden by event mode)
            float effectiveSpawnInterval = _activeEventMode?.SpawnIntervalOverride ?? spawnInterval;
            int effectiveMaxStars = _activeEventMode?.MaxStarsOverride ?? (limitActiveStars ? maxActiveStars : MAX_STARS);

            // Try to spawn new stars (may spawn multiple per frame if interval is very short)
            while (Time.time >= _nextSpawnTime && _activeStars.Count < effectiveMaxStars)
            {
                TrySpawnStar();
                // Clamp variance to not exceed interval, and ensure positive result
                float clampedVariance = Mathf.Min(spawnIntervalVariance, effectiveSpawnInterval * 0.9f);
                float nextInterval = effectiveSpawnInterval + UnityEngine.Random.Range(-clampedVariance, clampedVariance);
                _nextSpawnTime += Mathf.Max(0.001f, nextInterval);  // Use += to properly catch up
            }

            // Update existing stars
            for (int i = _activeStars.Count - 1; i >= 0; i--)
            {
                var star = _activeStars[i];
                star.UpdatePosition();

                // Check if star is in view (with trail margin)
                bool isInView = IsInCameraView(star.position, star.trailLength);

                // Behavior-specific state updates
                if (star.behavior.behaviorType == ShootingStarBehaviorType.Persistent)
                {
                    // Track when star first enters view
                    if (isInView && !star.behavior.HasEnteredView)
                    {
                        star.behavior.SetHasEnteredView();
                    }

                    // Start exit fade when leaving view (after having entered)
                    // Also start fade if star is outside view and has traveled past 40% of its journey
                    // (fallback for edge cases where star never registered as "in view")
                    bool shouldStartFade = !isInView && star.exitFadeStartTime == 0f &&
                        (star.behavior.HasEnteredView || star.Progress > 0.4f);

                    if (shouldStartFade)
                    {
                        star.exitFadeStartTime = Time.time;
                        star.behavior.SetIsExitFading();
                    }
                }

                _activeStars[i] = star;

                // Determine if star should be removed
                bool shouldRemove = ShouldRemoveStar(star, isInView);

                if (shouldRemove)
                {
                    _activeStars.RemoveAt(i);
                }
            }

            // Update shader
            ConfigureMaterial(_material);
        }

        private bool ShouldRemoveStar(ShootingStarData star, bool isInView)
        {
            switch (star.behavior.behaviorType)
            {
                case ShootingStarBehaviorType.Standard:
                    return star.IsComplete;

                case ShootingStarBehaviorType.Persistent:
                    // Failsafe: remove if lifetime exceeded (star never entered view)
                    // Normal: remove when fully faded after exiting view
                    return star.IsComplete || (star.behavior.IsExitFading && star.CalculateOpacity(isInView) <= 0f);

                case ShootingStarBehaviorType.SlowFade:
                    return star.CalculateOpacity(isInView) <= 0f;

                case ShootingStarBehaviorType.SimpleTimeFade:
                    return star.CalculateOpacity(isInView) <= 0f;

                default:
                    return star.IsComplete;
            }
        }

        /// <summary>
        /// Check if a position in world-space is within the camera's visible area.
        /// </summary>
        /// <param name="worldPos">Position in world coordinate space.</param>
        /// <param name="margin">Extra margin in world units.</param>
        private bool IsInCameraView(Vector2 worldPos, float margin = 0f)
        {
            if (_camera == null) return true;

            Vector2 camPos = _camera.transform.position;
            float halfHeight = _camera.orthographicSize + margin;
            float halfWidth = halfHeight * _camera.aspect;

            return Mathf.Abs(worldPos.x - camPos.x) <= halfWidth &&
                   Mathf.Abs(worldPos.y - camPos.y) <= halfHeight;
        }

        public override void ConfigureMaterial(Material material)
        {
            material.SetColor(StarColorID, starColor);
            material.SetFloat(BrightnessID, brightness);
            material.SetFloat(ParallaxFactorID, parallaxDepth);

            if (_camera != null)
            {
                material.SetFloat(CameraOrthoSizeID, _camera.orthographicSize);
            }

            // Ensure arrays are properly sized
            if (_positionArray == null || _positionArray.Length != MAX_STARS)
                _positionArray = new Vector4[MAX_STARS];
            if (_paramsArray == null || _paramsArray.Length != MAX_STARS)
                _paramsArray = new Vector4[MAX_STARS];

            // Pack star data into arrays
            int count = Mathf.Min(_activeStars.Count, MAX_STARS);
            material.SetInt(ActiveStarCountID, count);

            // Get current camera position for parallax calculation
            Vector2 currentCamPos = _camera != null ? (Vector2)_camera.transform.position : Vector2.zero;

            for (int i = 0; i < MAX_STARS; i++)
            {
                if (i < count)
                {
                    var star = _activeStars[i];
                    bool isInView = IsInCameraView(star.position, star.trailLength);
                    float opacity = star.CalculateOpacity(isInView);

                    // Calculate parallax-adjusted positions
                    // Zoom depth scaling is now handled in the shader (using effectiveOrthoSize)
                    // Distant stars (low parallaxDepth) should move WITH the camera (appear stationary)
                    // Near stars (high parallaxDepth) should stay in world space (drift backward)
                    Vector2 cameraDelta = currentCamPos - star.spawnCameraPosition;
                    Vector2 parallaxOffset = cameraDelta * (1f - parallaxDepth);
                    Vector2 apparentHead = star.position + parallaxOffset;
                    Vector2 apparentTail = star.TailPosition + parallaxOffset;

                    // xy = head position, zw = tail position (parallax-adjusted only)
                    _positionArray[i] = new Vector4(apparentHead.x, apparentHead.y, apparentTail.x, apparentTail.y);
                    // x = brightness (pre-multiplied with opacity), y = progress, z = per-star width, w = behavior type
                    _paramsArray[i] = new Vector4(star.brightness * opacity, star.Progress, star.width, (int)star.behavior.behaviorType);
                }
                else
                {
                    _positionArray[i] = Vector4.zero;
                    _paramsArray[i] = Vector4.zero;
                }
            }

            material.SetVectorArray(StarPositionsID, _positionArray);
            material.SetVectorArray(StarParamsID, _paramsArray);
        }

        private ShootingStarBehaviorConfig SelectBehaviorConfig()
        {
            // Use event mode configs if active and has overrides
            var configs = (_activeEventMode != null && _activeEventMode.HasBehaviorOverrides)
                ? _activeEventMode.BehaviorOverrides
                : behaviorConfigs;

            if (configs == null || configs.Count == 0)
                return null;

            if (configs.Count == 1)
                return configs[0];

            // Always recalculate total weight to handle inspector changes
            // (caching caused issues when configs were modified at runtime)
            float totalWeight = 0f;
            foreach (var config in configs)
            {
                if (config != null)
                    totalWeight += config.SelectionWeight;
            }

            if (totalWeight <= 0f)
                return configs[0];

            // Weighted UnityEngine.Random selection
            float Random = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var config in configs)
            {
                if (config == null) continue;
                cumulative += config.SelectionWeight;
                if (Random < cumulative)
                    return config;
            }

            return configs[configs.Count - 1];
        }

        /// <summary>
        /// Calculate direction for a star spawned at the given world-space position.
        /// Direction points toward a random target inside the visible area.
        /// </summary>
        /// <param name="worldSpawnPos">Spawn position in world coordinate space.</param>
        /// <summary>
        /// Calculate the effective orthographic size for this layer's depth.
        /// Distant layers (low parallax) use a smaller effective size, matching shader behavior.
        /// </summary>
        private float GetEffectiveOrthoSize()
        {
            float referenceZoom = Shader.GetGlobalFloat(ReferenceZoomID);
            if (referenceZoom <= 0f) referenceZoom = 10f;
            float zoomFactor = _camera.orthographicSize / referenceZoom;
            float depthZoomFactor = Mathf.Lerp(1f, zoomFactor, Mathf.Clamp01(parallaxDepth * 10f));
            return referenceZoom * depthZoomFactor;
        }

        private Vector2 CalculateDirection(Vector2 worldSpawnPos)
        {
            // Use event mode direction override if active
            if (_activeEventMode?.DirectionOverride != null)
            {
                return _activeEventMode.DirectionOverride.Value;
            }

            Vector2 camPos = _camera.transform.position;
            float halfHeight = GetEffectiveOrthoSize();
            float halfWidth = halfHeight * _camera.aspect;

            // Target inside visible area (0.8x to ensure it's well within bounds)
            Vector2 target = camPos + new Vector2(
                UnityEngine.Random.Range(-halfWidth * 0.8f, halfWidth * 0.8f),
                UnityEngine.Random.Range(-halfHeight * 0.8f, halfHeight * 0.8f)
            );

            // Base direction toward target in world space
            Vector2 baseDirection = (target - worldSpawnPos).normalized;
            float baseAngleRad = Mathf.Atan2(baseDirection.y, baseDirection.x);

            // Apply noise and variance as angular offsets
            // Use world position scaled for noise sampling
            float noiseValue = NoiseUtility.Perlin2D(worldSpawnPos, noiseScale);
            float noiseAngleOffset = noiseValue * 180f * noiseInfluence * Mathf.Deg2Rad;
            float randomOffset = UnityEngine.Random.Range(-angleVariance, angleVariance) * Mathf.Deg2Rad;

            float finalAngle = baseAngleRad + noiseAngleOffset + randomOffset;

            return new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
        }

        private void TrySpawnStar()
        {
            if (_camera == null) return;

            // Use effective ortho size to match shader's depth-adjusted view
            Vector2 camPos = _camera.transform.position;
            float effectiveOrtho = GetEffectiveOrthoSize();
            float halfHeight = effectiveOrtho;
            float halfWidth = halfHeight * _camera.aspect;

            // Spawn just outside visible area (1.2x margin ensures off-screen)
            float edgeAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spawnMargin = Mathf.Max(halfWidth, halfHeight) * 1.2f;

            // Spawn position in world space
            Vector2 spawnPos = camPos + new Vector2(
                Mathf.Cos(edgeAngle) * spawnMargin,
                Mathf.Sin(edgeAngle) * spawnMargin
            );

            Vector2 direction = CalculateDirection(spawnPos);

            // Calculate travel distance in world units
            // Diagonal of visible area plus spawn margin on both ends
            float diagonal = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) * 2f;
            float travelDistance = diagonal + spawnMargin * 2f;

            // Speed in world units per second
            float randomT = UnityEngine.Random.Range(0f, 1f);
            float curvedT = speedDistribution.Evaluate(randomT);
            float baseWorldSpeed = Mathf.Lerp(speedMin, speedMax, curvedT);

            // Scale speed by parallax depth for apparent distance effect
            // Distant stars (low parallax) appear to move slower
            float worldSpeed = baseWorldSpeed * parallaxDepth;

            // Lifetime based on world-space travel (using parallax-scaled speed)
            float lifetime = travelDistance / worldSpeed;

            // Trail length in world units (using effective ortho to match shader view)
            float worldTrailLength = (trailLength + UnityEngine.Random.Range(-trailLengthVariance, trailLengthVariance)) * effectiveOrtho * 2f;
            worldTrailLength = Mathf.Max(0.01f, worldTrailLength);

            // Calculate speed ratio (0-1) for brightness calculation (using base speed, not parallax-scaled)
            float speedRatio = Mathf.InverseLerp(speedMin, speedMax, baseWorldSpeed);

            // Apply curve exponent for speed-to-brightness mapping
            float speedFactor = Mathf.Pow(speedRatio, speedBrightnessCurve);

            // Calculate final brightness with speed factor and variance
            float starBrightness = brightness * speedFactor * (1f + UnityEngine.Random.Range(-brightnessVariance, brightnessVariance));
            float starWidth = UnityEngine.Random.Range(starWidthMin, starWidthMax);

            // Select behavior
            var behaviorConfig = SelectBehaviorConfig();
            var behaviorData = behaviorConfig?.CreateBehaviorData() ?? ShootingStarBehaviorData.CreateDefault();

            // Adjust lifetime for persistent behavior
            if (behaviorData.behaviorType == ShootingStarBehaviorType.Persistent)
            {
                lifetime *= persistentLifetimeMultiplier;
            }

            // Create the star with world-space values
            Vector2 cameraPos = _camera.transform.position;
            var star = ShootingStarData.Create(
                spawnPos,
                direction,
                worldSpeed,
                lifetime,
                starBrightness,
                worldTrailLength,
                starWidth,
                behaviorData,
                cameraPos
            );

            _activeStars.Add(star);
        }

        #region Runtime API

        /// <summary>
        /// Set an event mode that overrides normal behavior configuration.
        /// </summary>
        /// <param name="config">The event mode configuration to apply.</param>
        public void SetEventMode(EventModeConfig config)
        {
            _activeEventMode = config;
        }

        /// <summary>
        /// Clear the active event mode, returning to normal configuration.
        /// </summary>
        public void ClearEventMode()
        {
            _activeEventMode = null;
        }

        /// <summary>
        /// Get the currently active event mode, if any.
        /// </summary>
        public EventModeConfig ActiveEventMode => _activeEventMode;

        /// <summary>
        /// Spawn multiple stars in a specific direction (for events like meteor showers).
        /// </summary>
        /// <param name="direction">Normalized direction for all spawned stars.</param>
        /// <param name="count">Number of stars to spawn.</param>
        /// <param name="behaviorOverride">Optional behavior to use for all spawned stars.</param>
        /// <returns>Number of stars actually spawned (may be less if at capacity).</returns>
        public int SpawnDirectional(Vector2 direction, int count, ShootingStarBehaviorConfig behaviorOverride = null)
        {
            int spawned = 0;
            for (int i = 0; i < count && _activeStars.Count < MAX_STARS; i++)
            {
                if (SpawnStarWithBehavior(direction, behaviorOverride))
                    spawned++;
            }
            return spawned;
        }

        /// <summary>
        /// Spawn a star with specific behavior override.
        /// </summary>
        /// <param name="overrideDirection">Optional specific direction (normalized).</param>
        /// <param name="behaviorOverride">Optional behavior config to use.</param>
        /// <returns>True if spawned, false if at max capacity.</returns>
        public bool SpawnStarWithBehavior(Vector2? overrideDirection = null, ShootingStarBehaviorConfig behaviorOverride = null)
        {
            if (_activeStars.Count >= MAX_STARS) return false;
            if (_camera == null) return false;

            // Use effective ortho size to match shader's depth-adjusted view
            Vector2 camPos = _camera.transform.position;
            float effectiveOrtho = GetEffectiveOrthoSize();
            float halfHeight = effectiveOrtho;
            float halfWidth = halfHeight * _camera.aspect;

            // Spawn just outside visible area (1.2x margin ensures off-screen)
            float edgeAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spawnMargin = Mathf.Max(halfWidth, halfHeight) * 1.2f;

            // Spawn position in world space
            Vector2 spawnPos = camPos + new Vector2(
                Mathf.Cos(edgeAngle) * spawnMargin,
                Mathf.Sin(edgeAngle) * spawnMargin
            );

            Vector2 direction;
            if (overrideDirection.HasValue)
            {
                direction = overrideDirection.Value.normalized;
            }
            else
            {
                direction = CalculateDirection(spawnPos);
            }

            // Calculate travel distance in world units
            float diagonal = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) * 2f;
            float travelDistance = diagonal + spawnMargin * 2f;

            // Speed in world units per second
            float randomT = UnityEngine.Random.Range(0f, 1f);
            float curvedT = speedDistribution.Evaluate(randomT);
            float baseWorldSpeed = Mathf.Lerp(speedMin, speedMax, curvedT);

            // Scale speed by parallax depth for apparent distance effect
            // Distant stars (low parallax) appear to move slower
            float worldSpeed = baseWorldSpeed * parallaxDepth;

            // Lifetime based on world-space travel (using parallax-scaled speed)
            float lifetime = travelDistance / worldSpeed;

            // Trail length in world units (using effective ortho to match shader view)
            float worldTrailLength = (trailLength + UnityEngine.Random.Range(-trailLengthVariance, trailLengthVariance)) * effectiveOrtho * 2f;
            worldTrailLength = Mathf.Max(0.01f, worldTrailLength);

            // Calculate speed ratio (0-1) for brightness calculation (using base speed, not parallax-scaled)
            float speedRatio = Mathf.InverseLerp(speedMin, speedMax, baseWorldSpeed);

            // Apply curve exponent for speed-to-brightness mapping
            float speedFactor = Mathf.Pow(speedRatio, speedBrightnessCurve);

            // Calculate final brightness with speed factor and variance
            float starBrightness = brightness * speedFactor * (1f + UnityEngine.Random.Range(-brightnessVariance, brightnessVariance));
            float starWidth = UnityEngine.Random.Range(starWidthMin, starWidthMax);

            // Get behavior data
            var behaviorData = behaviorOverride?.CreateBehaviorData()
                ?? SelectBehaviorConfig()?.CreateBehaviorData()
                ?? ShootingStarBehaviorData.CreateDefault();

            // Adjust lifetime for persistent behavior
            if (behaviorData.behaviorType == ShootingStarBehaviorType.Persistent)
            {
                lifetime *= persistentLifetimeMultiplier;
            }

            // Create the star with world-space values
            Vector2 currentCamPos = _camera.transform.position;
            var star = ShootingStarData.Create(
                spawnPos,
                direction,
                worldSpeed,
                lifetime,
                starBrightness,
                worldTrailLength,
                starWidth,
                behaviorData,
                currentCamPos
            );

            _activeStars.Add(star);
            return true;
        }

        /// <summary>
        /// Manually spawn a shooting star (for game events, power-ups, etc.)
        /// </summary>
        /// <param name="overrideDirection">Optional specific direction (normalized)</param>
        /// <returns>True if spawned, false if at max capacity</returns>
        public bool SpawnStar(Vector2? overrideDirection = null)
        {
            return SpawnStarWithBehavior(overrideDirection, null);
        }

        /// <summary>
        /// Get the number of currently active shooting stars.
        /// </summary>
        public int ActiveStarCount => _activeStars?.Count ?? 0;

        /// <summary>
        /// Get current behavior distribution for debugging.
        /// </summary>
        public Dictionary<ShootingStarBehaviorType, int> GetActiveBehaviorCounts()
        {
            var counts = new Dictionary<ShootingStarBehaviorType, int>();
            if (_activeStars == null) return counts;

            foreach (var star in _activeStars)
            {
                if (!counts.ContainsKey(star.behavior.behaviorType))
                    counts[star.behavior.behaviorType] = 0;
                counts[star.behavior.behaviorType]++;
            }
            return counts;
        }

        /// <summary>
        /// No longer needed - weights are recalculated each spawn.
        /// Kept for API compatibility.
        /// </summary>
        [Obsolete("Weights are now recalculated each spawn. This method does nothing.")]
        public void InvalidateBehaviorWeights()
        {
            // No-op: weights are recalculated each spawn to support runtime inspector changes
        }

        #endregion

        #region Debug Gizmos

        /// <summary>
        /// Draw debug gizmos for this layer. Call from StarfieldManager.OnDrawGizmos().
        /// </summary>
        public void DrawGizmos()
        {
            if (!showSceneGizmos || _activeStars == null || _activeStars.Count == 0) return;
            if (_camera == null) return;

            // Calculate parallax offset (same formula as ConfigureMaterial)
            Vector2 currentCamPos = _camera.transform.position;

            foreach (var star in _activeStars)
            {
                // Calculate parallax offset for this star (matches ConfigureMaterial calculation)
                // Note: Zoom depth scaling is handled in the shader, not here
                Vector2 cameraDelta = currentCamPos - star.spawnCameraPosition;
                Vector2 parallaxOffset = cameraDelta * (1f - parallaxDepth);

                // Calculate destination based on direction and remaining travel
                float remainingTime = star.lifetime - (Time.time - star.spawnTime);
                Vector2 destination = star.position + star.direction * star.speed * Mathf.Max(0, remainingTime);

                // Apply parallax offset to get apparent positions
                Vector2 apparentSpawn = star.startPosition + parallaxOffset;
                Vector2 apparentCurrent = star.position + parallaxOffset;
                Vector2 apparentDest = destination + parallaxOffset;

                Vector3 worldSpawn = new Vector3(apparentSpawn.x, apparentSpawn.y, 0);
                Vector3 worldCurrent = new Vector3(apparentCurrent.x, apparentCurrent.y, 0);
                Vector3 worldDest = new Vector3(apparentDest.x, apparentDest.y, 0);

                // Draw spawn point (wire sphere)
                Gizmos.color = gizmoSpawnColor;
                Gizmos.DrawWireSphere(worldSpawn, 0.5f);

                // Draw current position (solid sphere)
                Gizmos.color = gizmoCurrentColor;
                Gizmos.DrawSphere(worldCurrent, 0.3f);

                // Draw destination (wire sphere)
                Gizmos.color = gizmoDestinationColor;
                Gizmos.DrawWireSphere(worldDest, 0.5f);

                // Draw path line: spawn -> current (green) -> destination (red)
                Gizmos.color = gizmoSpawnColor;
                Gizmos.DrawLine(worldSpawn, worldCurrent);
                Gizmos.color = gizmoDestinationColor;
                Gizmos.DrawLine(worldCurrent, worldDest);
            }
        }

        #endregion

        public override void Cleanup()
        {
            _activeStars?.Clear();
            _activeEventMode = null;
            base.Cleanup();
        }
    }
}
