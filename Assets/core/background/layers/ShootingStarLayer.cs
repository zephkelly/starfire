using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.Noise;

namespace Starfire.Core.Background.Layers
{
    /// <summary>
    /// A layer that spawns and renders shooting stars.
    /// Stars originate outside the camera view and travel until they exit.
    /// Direction is influenced by Perlin noise for regional variation.
    /// </summary>
    [System.Serializable]
    public class ShootingStarLayer : StarfieldLayer
    {
        private const int MAX_STARS = 8;

        [Header("Spawning")]
        [Tooltip("Average seconds between spawn attempts")]
        public float spawnInterval = 3f;

        [Tooltip("Random variance in spawn timing")]
        public float spawnIntervalVariance = 2f;

        [Tooltip("Maximum simultaneous shooting stars")]
        [Range(1, 8)]
        public int maxActiveStars = 5;

        [Header("Movement")]
        [Tooltip("Base speed in world units per second")]
        public float speed = 30f;

        [Tooltip("Random speed variance")]
        public float speedVariance = 15f;

        [Tooltip("Trail length in world units")]
        public float trailLength = 3f;

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

        [Header("Appearance")]
        public Color starColor = Color.white;

        [Tooltip("Base brightness of the shooting star")]
        [Range(0.5f, 5f)]
        public float brightness = 1.5f;

        [Tooltip("Random brightness variance (0 = all same brightness)")]
        [Range(0f, 1f)]
        public float brightnessVariance = 0.3f;

        [Tooltip("Minimum width of the shooting star")]
        [Min(0.01f)]
        public float starWidthMin = 0.3f;

        [Tooltip("Maximum width of the shooting star")]
        [Min(0.01f)]
        public float starWidthMax = 0.7f;

        // Runtime state
        [System.NonSerialized] private List<ShootingStarData> _activeStars = new List<ShootingStarData>();
        [System.NonSerialized] private float _nextSpawnTime;
        [System.NonSerialized] private Camera _camera;

        // Shader property IDs
        private static readonly int ActiveStarCountID = Shader.PropertyToID("_ActiveStarCount");
        private static readonly int StarPositionsID = Shader.PropertyToID("_StarPositions");
        private static readonly int StarParamsID = Shader.PropertyToID("_StarParams");
        private static readonly int StarColorID = Shader.PropertyToID("_StarColor");
        private static readonly int BrightnessID = Shader.PropertyToID("_Brightness");
        private static readonly int ParallaxFactorID = Shader.PropertyToID("_ParallaxFactor");
        private static readonly int CameraOrthoSizeID = Shader.PropertyToID("_CameraOrthoSize");

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
            _nextSpawnTime = Time.time + Random.Range(0f, spawnInterval);
            _camera = Camera.main;
        }

        public override void Update()
        {
            if (_material == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            // Try to spawn new stars
            if (Time.time >= _nextSpawnTime && _activeStars.Count < maxActiveStars)
            {
                TrySpawnStar();
                _nextSpawnTime = Time.time + spawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
            }

            // Update existing stars
            for (int i = _activeStars.Count - 1; i >= 0; i--)
            {
                var star = _activeStars[i];
                star.UpdatePosition();
                _activeStars[i] = star;

                // Remove completed stars
                if (star.IsComplete)
                {
                    _activeStars.RemoveAt(i);
                }
            }

            // Update shader
            ConfigureMaterial(_material);
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

            // Pack star data into arrays
            int count = Mathf.Min(_activeStars.Count, MAX_STARS);
            material.SetInt(ActiveStarCountID, count);

            for (int i = 0; i < MAX_STARS; i++)
            {
                if (i < count)
                {
                    var star = _activeStars[i];
                    // xy = head position, zw = tail position
                    _positionArray[i] = new Vector4(star.position.x, star.position.y, star.TailPosition.x, star.TailPosition.y);
                    // x = brightness, y = progress, z = per-star width
                    _paramsArray[i] = new Vector4(star.brightness, star.Progress, star.width, 0);
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

        private void TrySpawnStar()
        {
            if (_camera == null) return;

            // Get camera bounds
            float camHeight = _camera.orthographicSize * 2f;
            float camWidth = camHeight * _camera.aspect;
            Vector2 camPos = _camera.transform.position;

            // Calculate spawn position outside view
            // Pick a random edge and position along it
            float edgeAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float margin = Mathf.Max(camWidth, camHeight) * 0.6f; // Spawn outside view

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

            // Calculate how long the star needs to travel to fully exit view
            // This is an approximation - shoot across the full diagonal
            float travelDistance = Mathf.Sqrt(camWidth * camWidth + camHeight * camHeight) + margin * 2f;
            float starSpeed = speed + Random.Range(-speedVariance, speedVariance);
            float lifetime = travelDistance / starSpeed;

            // Calculate per-star brightness and width with variance
            float starBrightness = brightness * (1f + Random.Range(-brightnessVariance, brightnessVariance));
            float starWidth = Random.Range(starWidthMin, starWidthMax);

            // Create the star
            var star = ShootingStarData.Create(
                spawnPos,
                direction,
                starSpeed,
                lifetime,
                starBrightness,
                trailLength,
                starWidth
            );

            _activeStars.Add(star);
        }

        /// <summary>
        /// Manually spawn a shooting star (for game events, power-ups, etc.)
        /// </summary>
        /// <param name="overrideDirection">Optional specific direction (normalized)</param>
        /// <returns>True if spawned, false if at max capacity</returns>
        public bool SpawnStar(Vector2? overrideDirection = null)
        {
            if (_activeStars.Count >= maxActiveStars) return false;
            if (_camera == null) return false;

            // Get camera bounds
            float camHeight = _camera.orthographicSize * 2f;
            float camWidth = camHeight * _camera.aspect;
            Vector2 camPos = _camera.transform.position;

            // Random spawn position outside view
            float edgeAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float margin = Mathf.Max(camWidth, camHeight) * 0.6f;

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

            float travelDistance = Mathf.Sqrt(camWidth * camWidth + camHeight * camHeight) + margin * 2f;
            float starSpeed = speed + Random.Range(-speedVariance, speedVariance);
            float lifetime = travelDistance / starSpeed;

            // Calculate per-star brightness and width with variance
            float starBrightness = brightness * (1f + Random.Range(-brightnessVariance, brightnessVariance));
            float starWidth = Random.Range(starWidthMin, starWidthMax);

            var star = ShootingStarData.Create(
                spawnPos,
                direction,
                starSpeed,
                lifetime,
                starBrightness,
                trailLength,
                starWidth
            );

            _activeStars.Add(star);
            return true;
        }

        /// <summary>
        /// Get the number of currently active shooting stars.
        /// </summary>
        public int ActiveStarCount => _activeStars?.Count ?? 0;

        public override void Cleanup()
        {
            _activeStars?.Clear();
            base.Cleanup();
        }
    }
}
