using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Core.Lighting
{
    /// <summary>
    /// Singleton manager that collects edge light data and pushes it to all registered edge-lit sprites.
    /// Add this component to a persistent GameObject in your scene (e.g., GameManager or Camera).
    /// </summary>
    [DefaultExecutionOrder(-100)]  // Ensure this runs before EdgeLitSprite and EdgeLightSource
    [ExecuteAlways]
    public class EdgeLightManager : MonoBehaviour
    {
        public static EdgeLightManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Maximum number of edge lights that can affect sprites simultaneously")]
        [SerializeField] private int _maxLights = 8;

        private readonly List<EdgeLightSource> _sources = new();
        private readonly List<EdgeLitSprite> _sprites = new();

        // Cached shader property arrays (pre-allocated to avoid GC)
        private Vector4[] _lightPositions;  // xyz = world pos, w = range
        private Vector4[] _lightColors;     // rgb = color, a = intensity
        private Vector4[] _lightParams;     // xy = direction, z = inner angle, w = outer angle
        private int _activeLightCount;

        // Cached shader property IDs
        private static readonly int LightPositionsID = Shader.PropertyToID("_EdgeLightPositions");
        private static readonly int LightColorsID = Shader.PropertyToID("_EdgeLightColors");
        private static readonly int LightParamsID = Shader.PropertyToID("_EdgeLightParams");
        private static readonly int LightCountID = Shader.PropertyToID("_EdgeLightCount");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[EdgeLightManager] Multiple instances detected. Destroying duplicate on {gameObject.name}");
                if (Application.isPlaying)
                {
                    Destroy(this);
                }
                else
                {
                    DestroyImmediate(this);
                }
                return;
            }

            Instance = this;
            InitializeArrays();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            InitializeArrays();
        }

        private void InitializeArrays()
        {
            _lightPositions = new Vector4[_maxLights];
            _lightColors = new Vector4[_maxLights];
            _lightParams = new Vector4[_maxLights];
        }

        /// <summary>
        /// Register an edge light source to contribute to edge lighting.
        /// </summary>
        public void Register(EdgeLightSource source)
        {
            if (source != null && !_sources.Contains(source))
            {
                _sources.Add(source);
            }
        }

        /// <summary>
        /// Unregister an edge light source.
        /// </summary>
        public void Unregister(EdgeLightSource source)
        {
            _sources.Remove(source);
        }

        /// <summary>
        /// Register an edge-lit sprite to receive lighting updates.
        /// </summary>
        public void Register(EdgeLitSprite sprite)
        {
            if (sprite != null && !_sprites.Contains(sprite))
            {
                _sprites.Add(sprite);
            }
        }

        /// <summary>
        /// Unregister an edge-lit sprite.
        /// </summary>
        public void Unregister(EdgeLitSprite sprite)
        {
            _sprites.Remove(sprite);
        }

        private void LateUpdate()
        {
            CollectLightData();
            UpdateAllSprites();
        }

        private void CollectLightData()
        {
            _activeLightCount = 0;

            for (int i = 0; i < _sources.Count && _activeLightCount < _maxLights; i++)
            {
                var source = _sources[i];
                if (source == null || !source.IsEnabled)
                {
                    continue;
                }

                Vector3 pos = source.WorldPosition;
                float range = source.Range;
                Color color = source.LightColor;
                float intensity = source.Intensity;
                Vector2 direction = source.LightDirection;
                float innerAngle = source.InnerAngle;
                float outerAngle = source.OuterAngle;

                _lightPositions[_activeLightCount] = new Vector4(pos.x, pos.y, pos.z, range);
                _lightColors[_activeLightCount] = new Vector4(color.r, color.g, color.b, intensity);
                _lightParams[_activeLightCount] = new Vector4(direction.x, direction.y, innerAngle, outerAngle);
                _activeLightCount++;
            }

            // Zero out unused slots to avoid stale data
            for (int i = _activeLightCount; i < _maxLights; i++)
            {
                _lightPositions[i] = Vector4.zero;
                _lightColors[i] = Vector4.zero;
                _lightParams[i] = new Vector4(0f, 1f, 0f, 360f);  // Default: up direction, omnidirectional
            }
        }

        private void UpdateAllSprites()
        {
            for (int i = _sprites.Count - 1; i >= 0; i--)
            {
                var sprite = _sprites[i];
                if (sprite == null)
                {
                    _sprites.RemoveAt(i);
                    continue;
                }

                sprite.UpdateLightData(_lightPositions, _lightColors, _lightParams, _activeLightCount);
            }
        }

        /// <summary>
        /// Get the current number of active edge light sources.
        /// </summary>
        public int ActiveLightCount => _activeLightCount;

        /// <summary>
        /// Get the current number of registered sprites.
        /// </summary>
        public int RegisteredSpriteCount => _sprites.Count;

        /// <summary>
        /// Get the maximum number of lights supported.
        /// </summary>
        public int MaxLights => _maxLights;

        // Expose property IDs for EdgeLitSprite to use
        internal static int GetLightPositionsID() => LightPositionsID;
        internal static int GetLightColorsID() => LightColorsID;
        internal static int GetLightParamsID() => LightParamsID;
        internal static int GetLightCountID() => LightCountID;
    }
}
