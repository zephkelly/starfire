using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Noise;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// A layer that spawns and renders comets with particle debris trails.
    /// Comets have a bright nucleus with coma glow and scattered particle trails.
    /// </summary>
    [System.Serializable]
    public class CometLayer : StarfieldLayer
    {
        private const int MAX_COMETS = 4;

        [Header("Spawning")]
        [Tooltip("Average seconds between spawn attempts")]
        public float spawnInterval = 15f;

        [Tooltip("Random variance in spawn timing")]
        public float spawnIntervalVariance = 10f;

        [Tooltip("Maximum simultaneous comets")]
        [Range(1, 4)]
        public int maxActiveComets = 3;

        [Header("Movement")]
        [Tooltip("Base speed in world units per second (slower than shooting stars)")]
        public float speed = 8f;

        [Tooltip("Random speed variance")]
        public float speedVariance = 4f;

        [Header("Trail")]
        [Tooltip("How many seconds of trail to show behind the comet")]
        [Range(0.1f, 2f)]
        public float trailTime = 0.5f;

        [Tooltip("Reference parallax depth for trail scaling (deeper layers get shorter trails)")]
        public float referenceParallaxDepth = 0.02f;

        [Header("Direction Noise")]
        [Tooltip("Perlin noise scale (smaller = larger regions with same direction)")]
        public float noiseScale = 0.05f;

        [Tooltip("How much noise affects direction (0-1)")]
        [Range(0f, 1f)]
        public float noiseInfluence = 0.5f;

        [Tooltip("Base direction in degrees (0=right, 90=up, 225=down-left)")]
        public float baseAngle = 225f;

        [Tooltip("Random angle variance on top of noise")]
        public float angleVariance = 30f;

        [Header("Nucleus")]
        [Tooltip("Size of the bright nucleus core")]
        [Min(0.01f)]
        public float nucleusSize = 0.15f;

        [Tooltip("Size of the fuzzy coma glow")]
        [Min(0.01f)]
        public float comaSize = 0.6f;

        [Tooltip("How soft the coma edge is (higher = softer)")]
        [Range(0.5f, 5f)]
        public float comaSoftness = 2f;

        [Header("Particle Trail")]
        [Tooltip("Number of particles per comet trail")]
        [Range(8, 48)]
        public int particleCount = 24;

        [Tooltip("Minimum particle size")]
        [Min(0.005f)]
        public float particleSizeMin = 0.02f;

        [Tooltip("Maximum particle size")]
        [Min(0.01f)]
        public float particleSizeMax = 0.08f;

        [Tooltip("How wide particles spread from center line")]
        [Min(0f)]
        public float particleSpread = 0.5f;

        [Tooltip("How quickly particles dim along trail (higher = faster fade)")]
        [Range(0.5f, 5f)]
        public float particleFadeRate = 2f;

        [Header("Appearance")]
        public Color cometColor = new Color(0.8f, 0.9f, 1f, 1f);

        [Tooltip("Base brightness of the comet")]
        [Range(0.5f, 8f)]
        public float brightness = 3f;

        [Header("Pixelization")]
        [Tooltip("Resolution for pixel-art effect (higher = more pixels)")]
        [Range(100f, 1000f)]
        public float pixels = 400f;

        [Header("Gradient")]
        [Tooltip("1D gradient texture for color banding (hot->cold left to right)")]
        public Texture2D gradientTexture;

        [Header("Sun Direction")]
        [Tooltip("Direction light comes FROM (normalized). Ion tail points opposite.")]
        public Vector2 sunDirection = new Vector2(-1f, -0.5f);

        [Header("Dual Tails")]
        [Tooltip("How much the dust tail curves perpendicular to travel")]
        [Range(0f, 1f)]
        public float dustTailCurve = 0.3f;

        [Tooltip("Width of the dust tail")]
        [Range(0.1f, 2f)]
        public float dustTailWidth = 0.8f;

        [Tooltip("Dust tail falloff curve")]
        [Range(1f, 5f)]
        public float dustFalloff = 2f;

        [Tooltip("Ion tail length relative to dust tail")]
        [Range(0.5f, 2f)]
        public float ionTailLengthMultiplier = 1.5f;

        [Tooltip("Width of the ion tail")]
        [Range(0.05f, 0.5f)]
        public float ionTailWidth = 0.2f;

        [Tooltip("Ion tail falloff curve")]
        [Range(1f, 5f)]
        public float ionFalloff = 1.5f;

        [Header("Tail Noise")]
        [Tooltip("Scale of FBM noise on tail edges")]
        public float tailNoiseScale = 50f;

        [Tooltip("FBM octaves for tail edge noise")]
        [Range(1, 5)]
        public int tailNoiseOctaves = 3;

        [Header("Core Animation")]
        [Tooltip("Speed of nucleus brightness pulsing")]
        [Range(0.5f, 5f)]
        public float corePulseSpeed = 1.5f;

        [Tooltip("Amount of nucleus size/brightness variation")]
        [Range(0f, 0.5f)]
        public float corePulseAmount = 0.15f;

        [Header("Boiling Front")]
        [Tooltip("Cell scale for boiling effect")]
        [Range(2f, 12f)]
        public float boilCellScale = 6f;

        [Tooltip("Animation speed of boiling effect")]
        [Range(0.5f, 5f)]
        public float boilSpeed = 2f;

        [Tooltip("Intensity of the boiling effect")]
        [Range(0f, 1f)]
        public float boilIntensity = 0.6f;

        [Header("Sparkles")]
        [Tooltip("Number of sparkle particles per comet")]
        [Range(8, 32)]
        public int sparkleCount = 16;

        [Tooltip("Size of sparkle particles")]
        [Range(0.005f, 0.03f)]
        public float sparkleSize = 0.015f;

        [Tooltip("Sparkle twinkle animation speed")]
        [Range(2f, 15f)]
        public float sparkleSpeed = 8f;

        [Tooltip("Brightness of sparkle particles")]
        [Range(0.5f, 3f)]
        public float sparkleBrightness = 1.5f;

        [Header("Seed")]
        [Tooltip("Seed for deterministic noise patterns")]
        [Range(1, 10)]
        public int seed = 1;

        // Runtime state
        [System.NonSerialized] private List<CometData> _activeComets = new List<CometData>();
        [System.NonSerialized] private float _nextSpawnTime;
        [System.NonSerialized] private Camera _camera;
        [System.NonSerialized] private int _seedCounter = 0;

        // Shader property IDs - existing
        private static readonly int ActiveCometCountID = Shader.PropertyToID("_ActiveCometCount");
        private static readonly int CometPositionsID = Shader.PropertyToID("_CometPositions");
        private static readonly int CometParams1ID = Shader.PropertyToID("_CometParams1");
        private static readonly int CometParams2ID = Shader.PropertyToID("_CometParams2");
        private static readonly int CometParams3ID = Shader.PropertyToID("_CometParams3");
        private static readonly int CometColorID = Shader.PropertyToID("_CometColor");
        private static readonly int BrightnessID = Shader.PropertyToID("_Brightness");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");
        private static readonly int CameraOrthoSizeID = Shader.PropertyToID("_CameraOrthoSize");
        private static readonly int NucleusSizeID = Shader.PropertyToID("_NucleusSize");
        private static readonly int ComaSizeID = Shader.PropertyToID("_ComaSize");
        private static readonly int ComaSoftnessID = Shader.PropertyToID("_ComaSoftness");
        private static readonly int ParticleCountID = Shader.PropertyToID("_ParticleCount");
        private static readonly int ParticleSizeMinID = Shader.PropertyToID("_ParticleSizeMin");
        private static readonly int ParticleSizeMaxID = Shader.PropertyToID("_ParticleSizeMax");
        private static readonly int ParticleSpreadID = Shader.PropertyToID("_ParticleSpread");
        private static readonly int ParticleFadeRateID = Shader.PropertyToID("_ParticleFadeRate");

        // Shader property IDs - new enhanced features
        private static readonly int PixelsID = Shader.PropertyToID("_Pixels");
        private static readonly int GradientTexID = Shader.PropertyToID("_GradientTex");
        private static readonly int SunDirectionID = Shader.PropertyToID("_SunDirection");
        private static readonly int DustTailWidthID = Shader.PropertyToID("_DustTailWidth");
        private static readonly int DustTailCurveID = Shader.PropertyToID("_DustTailCurve");
        private static readonly int DustFalloffID = Shader.PropertyToID("_DustFalloff");
        private static readonly int IonTailWidthID = Shader.PropertyToID("_IonTailWidth");
        private static readonly int IonTailLengthID = Shader.PropertyToID("_IonTailLength");
        private static readonly int IonFalloffID = Shader.PropertyToID("_IonFalloff");
        private static readonly int TailNoiseScaleID = Shader.PropertyToID("_TailNoiseScale");
        private static readonly int TailNoiseOctavesID = Shader.PropertyToID("_TailNoiseOctaves");
        private static readonly int CorePulseSpeedID = Shader.PropertyToID("_CorePulseSpeed");
        private static readonly int CorePulseAmountID = Shader.PropertyToID("_CorePulseAmount");
        private static readonly int BoilCellScaleID = Shader.PropertyToID("_BoilCellScale");
        private static readonly int BoilSpeedID = Shader.PropertyToID("_BoilSpeed");
        private static readonly int BoilIntensityID = Shader.PropertyToID("_BoilIntensity");
        private static readonly int SparkleCountID = Shader.PropertyToID("_SparkleCount");
        private static readonly int SparkleSizeID = Shader.PropertyToID("_SparkleSize");
        private static readonly int SparkleSpeedID = Shader.PropertyToID("_SparkleSpeed");
        private static readonly int SparkleBrightnessID = Shader.PropertyToID("_SparkleBrightness");
        private static readonly int SeedID = Shader.PropertyToID("_Seed");

        // Arrays for passing to shader
        private Vector4[] _positionArray = new Vector4[MAX_COMETS];
        private Vector4[] _params1Array = new Vector4[MAX_COMETS];
        private Vector4[] _params2Array = new Vector4[MAX_COMETS];
        private Vector4[] _params3Array = new Vector4[MAX_COMETS];

        public override Shader GetShader()
        {
            return Shader.Find("Starfire/Comet");
        }

        public override void Initialize(Transform parent, Mesh quadMesh, int sortOrder)
        {
            base.Initialize(parent, quadMesh, sortOrder);

            _activeComets = new List<CometData>();
            _nextSpawnTime = Time.time + Random.Range(0f, spawnInterval);
            _camera = Camera.main;
            _seedCounter = Random.Range(0, 10000);
        }

        public override void Update()
        {
            if (_material == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            // Try to spawn new comets
            if (Time.time >= _nextSpawnTime && _activeComets.Count < maxActiveComets)
            {
                TrySpawnComet();
                _nextSpawnTime = Time.time + spawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
            }

            // Update existing comets
            for (int i = _activeComets.Count - 1; i >= 0; i--)
            {
                var comet = _activeComets[i];
                comet.UpdatePosition();
                _activeComets[i] = comet;

                // Calculate apparent position (where the comet APPEARS on screen after parallax)
                Vector2 apparentPos = GetApparentPosition(comet.position, comet.spawnCameraPosition);

                // Remove comets that are complete OR outside the kill zone
                // Use apparent position to match where the comet is visually rendered
                bool outsideKillZone = IsOutsideKillZone(apparentPos);
                if (comet.IsComplete || outsideKillZone)
                {
                    _activeComets.RemoveAt(i);
                }
            }

            // Update shader
            ConfigureMaterial(_material);
        }

        public override void ConfigureMaterial(Material material)
        {
            // Basic appearance
            material.SetColor(CometColorID, cometColor);
            material.SetFloat(BrightnessID, brightness);
            material.SetFloat(ParallaxFactorID, parallaxDepth);
            material.SetFloat(NucleusSizeID, nucleusSize);
            material.SetFloat(ComaSizeID, comaSize);
            material.SetFloat(ComaSoftnessID, comaSoftness);
            material.SetInt(ParticleCountID, particleCount);
            material.SetFloat(ParticleSizeMinID, particleSizeMin);
            material.SetFloat(ParticleSizeMaxID, particleSizeMax);
            material.SetFloat(ParticleSpreadID, particleSpread);
            material.SetFloat(ParticleFadeRateID, particleFadeRate);

            // Pixelization
            material.SetFloat(PixelsID, pixels);

            // Gradient texture
            if (gradientTexture != null)
            {
                material.SetTexture(GradientTexID, gradientTexture);
            }

            // Sun direction for ion tail
            Vector2 normalizedSunDir = sunDirection.normalized;
            material.SetVector(SunDirectionID, new Vector4(normalizedSunDir.x, normalizedSunDir.y, 0, 0));

            // Dual tails
            material.SetFloat(DustTailWidthID, dustTailWidth);
            material.SetFloat(DustTailCurveID, dustTailCurve);
            material.SetFloat(DustFalloffID, dustFalloff);
            material.SetFloat(IonTailWidthID, ionTailWidth);
            material.SetFloat(IonTailLengthID, ionTailLengthMultiplier);
            material.SetFloat(IonFalloffID, ionFalloff);

            // Tail noise
            material.SetFloat(TailNoiseScaleID, tailNoiseScale);
            material.SetInt(TailNoiseOctavesID, tailNoiseOctaves);

            // Core animation
            material.SetFloat(CorePulseSpeedID, corePulseSpeed);
            material.SetFloat(CorePulseAmountID, corePulseAmount);

            // Boiling front
            material.SetFloat(BoilCellScaleID, boilCellScale);
            material.SetFloat(BoilSpeedID, boilSpeed);
            material.SetFloat(BoilIntensityID, boilIntensity);

            // Sparkles
            material.SetInt(SparkleCountID, sparkleCount);
            material.SetFloat(SparkleSizeID, sparkleSize);
            material.SetFloat(SparkleSpeedID, sparkleSpeed);
            material.SetFloat(SparkleBrightnessID, sparkleBrightness);

            // Seed
            material.SetFloat(SeedID, seed);

            if (_camera != null)
            {
                material.SetFloat(CameraOrthoSizeID, _camera.orthographicSize);
            }

            // Pack comet data into arrays
            int count = Mathf.Min(_activeComets.Count, MAX_COMETS);
            material.SetInt(ActiveCometCountID, count);

            for (int i = 0; i < MAX_COMETS; i++)
            {
                if (i < count)
                {
                    var comet = _activeComets[i];
                    // xy = head position, zw = tail position
                    _positionArray[i] = new Vector4(comet.position.x, comet.position.y, comet.TailPosition.x, comet.TailPosition.y);
                    // x = brightness, y = progress, z = nucleusSize, w = comaSize
                    _params1Array[i] = new Vector4(comet.brightness, comet.Progress, comet.nucleusSize, comet.comaSize);
                    // x = particleSeed, y = speed, z = sunOverrideX, w = sunOverrideY (0,0 = use global)
                    _params2Array[i] = new Vector4(comet.particleSeed, comet.speed, 0, 0);
                    // x = pulsePhase, y = dustCurveAmount, z = ionLengthMult, w = unused
                    _params3Array[i] = new Vector4(comet.pulsePhase, comet.dustCurveAmount, comet.ionLengthMult, 0);
                }
                else
                {
                    _positionArray[i] = Vector4.zero;
                    _params1Array[i] = Vector4.zero;
                    _params2Array[i] = Vector4.zero;
                    _params3Array[i] = Vector4.zero;
                }
            }

            material.SetVectorArray(CometPositionsID, _positionArray);
            material.SetVectorArray(CometParams1ID, _params1Array);
            material.SetVectorArray(CometParams2ID, _params2Array);
            material.SetVectorArray(CometParams3ID, _params3Array);
        }

        /// <summary>
        /// Calculate the apparent (parallax-adjusted) position of a comet.
        /// This is where the comet appears on screen, accounting for camera movement since spawn.
        /// </summary>
        private Vector2 GetApparentPosition(Vector2 worldPos, Vector2 spawnCameraPosition)
        {
            if (_camera == null) return worldPos;

            Vector2 currentCamPos = _camera.transform.position;
            Vector2 cameraDelta = currentCamPos - spawnCameraPosition;
            Vector2 parallaxOffset = cameraDelta * (1f - parallaxDepth);
            return worldPos + parallaxOffset;
        }

        /// <summary>
        /// Check if a position is outside the kill zone boundary.
        /// Comets outside this zone are forcibly removed regardless of lifetime.
        /// </summary>
        private bool IsOutsideKillZone(Vector2 worldPos)
        {
            if (_camera == null) return false;

            Vector2 camPos = _camera.transform.position;
            // Use the same effective area calculation as spawning, with extra margin
            float effectiveHalfHeight = _camera.orthographicSize / parallaxDepth;
            float effectiveHalfWidth = effectiveHalfHeight * _camera.aspect;
            // Kill zone is 2x the spawn margin to allow comets to fully traverse
            float killZoneMargin = 2.5f;

            return Mathf.Abs(worldPos.x - camPos.x) > effectiveHalfWidth * killZoneMargin ||
                   Mathf.Abs(worldPos.y - camPos.y) > effectiveHalfHeight * killZoneMargin;
        }

        private void TrySpawnComet()
        {
            if (_camera == null) return;

            Vector2 camPos = _camera.transform.position;

            // Calculate spawn position outside the shader visible area
            // In shader: positions are scaled by parallax, visible range is ±orthoSize
            // Therefore world-space visible area = orthoSize / parallax
            float effectiveHalfHeight = _camera.orthographicSize / parallaxDepth;
            float effectiveHalfWidth = effectiveHalfHeight * _camera.aspect;

            // Spawn just outside this area (1.2x margin ensures off-screen)
            float edgeAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float margin = Mathf.Max(effectiveHalfWidth, effectiveHalfHeight) * 1.2f;

            Vector2 spawnPos = camPos + new Vector2(
                Mathf.Cos(edgeAngle) * margin,
                Mathf.Sin(edgeAngle) * margin
            );

            // Calculate direction using noise
            float noiseValue = NoiseUtility.Perlin2D(spawnPos, noiseScale);
            float noiseAngleOffset = noiseValue * 180f * noiseInfluence;
            float randomOffset = Random.Range(-angleVariance, angleVariance);
            float finalAngle = (baseAngle + noiseAngleOffset + randomOffset) * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));

            // Calculate speed and dynamic trail length
            float cometSpeed = speed + Random.Range(-speedVariance, speedVariance);

            // Trail length = time * speed * parallax factor
            // Deeper layers (smaller parallaxDepth) get shorter trails
            float parallaxFactor = parallaxDepth / referenceParallaxDepth;
            float dynamicTrailLength = trailTime * cometSpeed * parallaxFactor;

            // Calculate how long the comet needs to travel to fully exit view
            // Diagonal of the effective visible area plus spawn margin on both ends
            float effectiveDiagonal = Mathf.Sqrt(effectiveHalfWidth * effectiveHalfWidth + effectiveHalfHeight * effectiveHalfHeight) * 2f;
            float travelDistance = effectiveDiagonal + margin * 2f + dynamicTrailLength;
            float lifetime = travelDistance / cometSpeed;

            // Generate unique seed for this comet's particles
            int particleSeed = _seedCounter++;

            // Create the comet
            var comet = CometData.Create(
                spawnPos,
                direction,
                cometSpeed,
                lifetime,
                brightness,
                dynamicTrailLength,
                nucleusSize,
                comaSize,
                particleSeed,
                camPos
            );

            _activeComets.Add(comet);
        }

        /// <summary>
        /// Manually spawn a comet (for game events, etc.)
        /// </summary>
        /// <param name="overrideDirection">Optional specific direction (normalized)</param>
        /// <returns>True if spawned, false if at max capacity</returns>
        public bool SpawnComet(Vector2? overrideDirection = null)
        {
            if (_activeComets.Count >= maxActiveComets) return false;
            if (_camera == null) return false;

            Vector2 camPos = _camera.transform.position;

            // Calculate spawn position outside the shader visible area
            // In shader: positions are scaled by parallax, visible range is ±orthoSize
            // Therefore world-space visible area = orthoSize / parallax
            float effectiveHalfHeight = _camera.orthographicSize / parallaxDepth;
            float effectiveHalfWidth = effectiveHalfHeight * _camera.aspect;

            // Spawn just outside this area (1.2x margin ensures off-screen)
            float edgeAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float margin = Mathf.Max(effectiveHalfWidth, effectiveHalfHeight) * 1.2f;

            Vector2 spawnPos = camPos + new Vector2(
                Mathf.Cos(edgeAngle) * margin,
                Mathf.Sin(edgeAngle) * margin
            );

            Vector2 direction;
            if (overrideDirection.HasValue)
            {
                direction = overrideDirection.Value.normalized;
            }
            else
            {
                float noiseValue = NoiseUtility.Perlin2D(spawnPos, noiseScale);
                float noiseAngleOffset = noiseValue * 180f * noiseInfluence;
                float randomOffset = Random.Range(-angleVariance, angleVariance);
                float finalAngle = (baseAngle + noiseAngleOffset + randomOffset) * Mathf.Deg2Rad;
                direction = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
            }

            // Calculate speed and dynamic trail length
            float cometSpeed = speed + Random.Range(-speedVariance, speedVariance);

            // Trail length = time * speed * parallax factor
            float parallaxFactor = parallaxDepth / referenceParallaxDepth;
            float dynamicTrailLength = trailTime * cometSpeed * parallaxFactor;

            // Diagonal of the effective visible area plus spawn margin on both ends
            float effectiveDiagonal = Mathf.Sqrt(effectiveHalfWidth * effectiveHalfWidth + effectiveHalfHeight * effectiveHalfHeight) * 2f;
            float travelDistance = effectiveDiagonal + margin * 2f + dynamicTrailLength;
            float lifetime = travelDistance / cometSpeed;

            int particleSeed = _seedCounter++;

            var comet = CometData.Create(
                spawnPos,
                direction,
                cometSpeed,
                lifetime,
                brightness,
                dynamicTrailLength,
                nucleusSize,
                comaSize,
                particleSeed,
                camPos
            );

            _activeComets.Add(comet);
            return true;
        }

        /// <summary>
        /// Get the number of currently active comets.
        /// </summary>
        public int ActiveCometCount => _activeComets?.Count ?? 0;

        public override void Cleanup()
        {
            _activeComets?.Clear();
            base.Cleanup();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_activeComets == null || _activeComets.Count == 0)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector2 camPos = cam.transform.position;

            // Draw spawn/kill zone boundaries
            float effectiveHalfHeight = cam.orthographicSize * (1f / parallaxDepth);
            float effectiveHalfWidth = effectiveHalfHeight * cam.aspect;
            float margin = Mathf.Max(effectiveHalfWidth, effectiveHalfHeight) * 1.2f;
            float spawnRadius = Mathf.Sqrt(effectiveHalfWidth * effectiveHalfWidth + effectiveHalfHeight * effectiveHalfHeight) + margin;
            float killRadius = spawnRadius + 50f;

            // Spawn zone (white)
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            DrawGizmoCircle(camPos, spawnRadius, 32);

            // Kill zone (red)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            DrawGizmoCircle(camPos, killRadius, 32);

            // Draw each comet
            foreach (var comet in _activeComets)
            {
                // Comet head position (yellow sphere)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(comet.position, 2f);
                Gizmos.DrawSphere(comet.position, 0.5f);

                // Comet tail position (orange sphere)
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Vector2 tailPos = comet.TailPosition;
                Gizmos.DrawWireSphere(tailPos, 1f);

                // Trail path (yellow line)
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(comet.position, tailPos);

                // Ion tail direction (blue line - opposite of sun direction)
                Gizmos.color = Color.cyan;
                Vector2 ionDir = -sunDirection.normalized;
                Gizmos.DrawLine(comet.position, (Vector2)comet.position + ionDir * comet.trailLength * 1.5f);

                // Movement direction (green arrow)
                Gizmos.color = Color.green;
                Gizmos.DrawLine(comet.position, (Vector2)comet.position + comet.direction * 5f);

                // Start position (magenta - where comet spawned)
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(comet.startPosition, 1f);

                // Line from start to current (magenta dashed concept)
                Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
                Gizmos.DrawLine(comet.startPosition, comet.position);

                // Draw label with debug info
                UnityEditor.Handles.Label(comet.position + Vector2.up * 3f,
                    $"Progress: {comet.Progress:F2}\n" +
                    $"Pos: ({comet.position.x:F1}, {comet.position.y:F1})\n" +
                    $"Speed: {comet.speed:F1}\n" +
                    $"Trail: {comet.trailLength:F1}");
            }

            // Draw camera view bounds for reference
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            float viewHalfHeight = cam.orthographicSize;
            float viewHalfWidth = viewHalfHeight * cam.aspect;
            Vector3 topLeft = new Vector3(camPos.x - viewHalfWidth, camPos.y + viewHalfHeight, 0);
            Vector3 topRight = new Vector3(camPos.x + viewHalfWidth, camPos.y + viewHalfHeight, 0);
            Vector3 bottomLeft = new Vector3(camPos.x - viewHalfWidth, camPos.y - viewHalfHeight, 0);
            Vector3 bottomRight = new Vector3(camPos.x + viewHalfWidth, camPos.y - viewHalfHeight, 0);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }

        private void DrawGizmoCircle(Vector2 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector2(radius, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
#endif
    }
}
