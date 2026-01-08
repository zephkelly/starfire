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
        [Range(0.5f, 5f)]
        public float brightness = 2f;

        // Runtime state
        [System.NonSerialized] private List<CometData> _activeComets = new List<CometData>();
        [System.NonSerialized] private float _nextSpawnTime;
        [System.NonSerialized] private Camera _camera;
        [System.NonSerialized] private int _seedCounter = 0;

        // Shader property IDs
        private static readonly int ActiveCometCountID = Shader.PropertyToID("_ActiveCometCount");
        private static readonly int CometPositionsID = Shader.PropertyToID("_CometPositions");
        private static readonly int CometParams1ID = Shader.PropertyToID("_CometParams1");
        private static readonly int CometParams2ID = Shader.PropertyToID("_CometParams2");
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

        // Arrays for passing to shader
        private Vector4[] _positionArray = new Vector4[MAX_COMETS];
        private Vector4[] _params1Array = new Vector4[MAX_COMETS];
        private Vector4[] _params2Array = new Vector4[MAX_COMETS];

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

                // Remove completed comets
                if (comet.IsComplete)
                {
                    _activeComets.RemoveAt(i);
                }
            }

            // Update shader
            ConfigureMaterial(_material);
        }

        public override void ConfigureMaterial(Material material)
        {
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
                    // x = particleSeed, y = speed (for particle spread scaling), z = unused, w = unused
                    _params2Array[i] = new Vector4(comet.particleSeed, comet.speed, 0, 0);
                }
                else
                {
                    _positionArray[i] = Vector4.zero;
                    _params1Array[i] = Vector4.zero;
                    _params2Array[i] = Vector4.zero;
                }
            }

            material.SetVectorArray(CometPositionsID, _positionArray);
            material.SetVectorArray(CometParams1ID, _params1Array);
            material.SetVectorArray(CometParams2ID, _params2Array);
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
                particleSeed
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
                particleSeed
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
    }
}
