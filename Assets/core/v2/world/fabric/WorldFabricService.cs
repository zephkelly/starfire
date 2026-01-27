using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace StarfireV2
{
    /// <summary>
    /// Central service managing world fabric layers.
    /// Integrates with existing WorldGenerationService.
    /// </summary>
    public class WorldFabricService : MonoBehaviour
    {
        public static WorldFabricService Instance { get; private set; }

        [SerializeField] private WorldFabricConfig config;

        private readonly List<IWorldLayer> _layers = new();
        private readonly Dictionary<string, IWorldLayer> _layersById = new();
        private readonly Dictionary<string, IWorldLayerQuery> _queryCache = new();

        private WorldGenerationService _worldService;
        private bool _initialized;

        // Edit-mode preview support
        private Dictionary<string, IWorldLayerQuery> _editModeQueries;
        private bool _editModeQueriesValid;

        public event Action<Chunk, string> OnLayerGenerated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
            if (Instance == this)
                Instance = null;
        }

        private void Initialize()
        {
            if (_initialized) return;

            _worldService = WorldGenerationService.Instance;
            if (_worldService == null)
            {
                Debug.LogWarning("WorldFabricService: WorldGenerationService not found. Will retry.");
                return;
            }

            // Subscribe to chunk events
            _worldService.OnChunkGenerated += HandleChunkGenerated;
            _worldService.OnChunkDestroyed += HandleChunkDestroyed;

            // Initialize layers from config
            InitializeLayers();

            _initialized = true;

            if (config != null && config.logLayerEvents)
                Debug.Log($"WorldFabricService initialized with {_layers.Count} layers");
        }

        private void Cleanup()
        {
            if (_worldService != null)
            {
                _worldService.OnChunkGenerated -= HandleChunkGenerated;
                _worldService.OnChunkDestroyed -= HandleChunkDestroyed;
            }

            _layers.Clear();
            _layersById.Clear();
            _queryCache.Clear();
            _initialized = false;
        }

        private void InitializeLayers()
        {
            if (config == null) return;

            // Register layers based on config
            if (config.spaceZoneConfig != null && config.spaceZoneConfig.enabled)
            {
                RegisterLayer(new SpaceZoneLayer(config.spaceZoneConfig));
            }

            if (config.resourceConfig != null && config.resourceConfig.enabled)
            {
                RegisterLayer(new ResourceLayer(config.resourceConfig));
            }

            if (config.factionConfig != null && config.factionConfig.enabled)
            {
                RegisterLayer(new FactionTerritoryLayer(config.factionConfig));
            }

            // Sort by priority
            _layers.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            // Validate dependencies
            ValidateDependencies();
        }

        public void RegisterLayer(IWorldLayer layer)
        {
            if (layer == null) return;
            if (_layersById.ContainsKey(layer.LayerId))
            {
                Debug.LogWarning($"WorldFabricService: Layer {layer.LayerId} already registered");
                return;
            }

            _layers.Add(layer);
            _layersById[layer.LayerId] = layer;

            // Cache query for runtime lookups
            var query = layer.CreateQuery();
            if (query != null)
                _queryCache[layer.LayerId] = query;

            if (config != null && config.logLayerEvents)
                Debug.Log($"WorldFabricService: Registered layer {layer.LayerId} (priority {layer.Priority})");
        }

        private void ValidateDependencies()
        {
            var dataTypes = new HashSet<Type>();
            foreach (var layer in _layers)
            {
                foreach (var dep in layer.Dependencies)
                {
                    if (!dataTypes.Contains(dep))
                    {
                        Debug.LogError($"WorldFabricService: Layer {layer.LayerId} depends on {dep.Name} but no earlier layer produces it");
                    }
                }
                dataTypes.Add(layer.DataType);
            }
        }

        private void HandleChunkGenerated(Chunk chunk)
        {
            if (!_initialized || _worldService == null) return;

            var context = new WorldFabricContext(
                _worldService.WorldSeed,
                chunk,
                _worldService.ChunkSize,
                _worldService.OriginOffset,
                _queryCache
            );

            // Process layers in priority order
            foreach (var layer in _layers)
            {
                if (!layer.IsEnabled) continue;

                try
                {
                    var data = layer.Generate(chunk, context);
                    if (data != null)
                    {
                        chunk.SetData(data);
                        context.AddLayerData(data);
                        OnLayerGenerated?.Invoke(chunk, layer.LayerId);

                        if (config != null && config.logLayerEvents)
                            Debug.Log($"WorldFabricService: Layer {layer.LayerId} generated for chunk {chunk.Coord}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"WorldFabricService: Layer {layer.LayerId} failed for chunk {chunk.Coord}: {e}");
                }
            }
        }

        private void HandleChunkDestroyed(Chunk chunk)
        {
            foreach (var layer in _layers)
            {
                if (!layer.IsEnabled) continue;
                if (!chunk.HasData(layer.DataType)) continue;

                try
                {
                    layer.OnChunkUnloading(chunk);
                }
                catch (Exception e)
                {
                    Debug.LogError($"WorldFabricService: Layer {layer.LayerId} cleanup failed for chunk {chunk.Coord}: {e}");
                }
            }
        }

        #region Runtime Query API

        /// <summary>
        /// Ensures edit-mode queries are available for preview.
        /// Call this before using queries in edit mode.
        /// </summary>
        public void EnsureEditModeQueries()
        {
            if (_editModeQueriesValid && _editModeQueries != null) return;

            _editModeQueries = new Dictionary<string, IWorldLayerQuery>();

            if (config == null) return;

            // Create temporary layers for edit-mode queries
            if (config.spaceZoneConfig != null && config.spaceZoneConfig.enabled)
            {
                var layer = new SpaceZoneLayer(config.spaceZoneConfig);
                _editModeQueries["SpaceZones"] = layer.CreateQuery();
            }

            if (config.resourceConfig != null && config.resourceConfig.enabled)
            {
                var layer = new ResourceLayer(config.resourceConfig);
                _editModeQueries["Resources"] = layer.CreateQuery();
            }

            if (config.factionConfig != null && config.factionConfig.enabled)
            {
                var layer = new FactionTerritoryLayer(config.factionConfig);
                _editModeQueries["FactionTerritory"] = layer.CreateQuery();
            }

            _editModeQueriesValid = true;
        }

        /// <summary>
        /// Invalidates edit-mode queries (call when config changes).
        /// </summary>
        public void InvalidateEditModeQueries()
        {
            _editModeQueriesValid = false;
            _editModeQueries = null;
        }

        private Dictionary<string, IWorldLayerQuery> GetActiveQueryCache()
        {
            // In play mode, use the runtime cache
            if (Application.isPlaying && _initialized)
                return _queryCache;

            // In edit mode, use the edit-mode queries
            EnsureEditModeQueries();
            return _editModeQueries ?? new Dictionary<string, IWorldLayerQuery>();
        }

        /// <summary>
        /// Get the SpaceZone query for direct zone sampling.
        /// Used by FabricNebulaChunkGenerator and other external consumers.
        /// </summary>
        public SpaceZoneQuery GetSpaceZoneQuery()
        {
            var cache = GetActiveQueryCache();
            if (cache.TryGetValue("SpaceZones", out var query) && query is SpaceZoneQuery szq)
                return szq;
            return null;
        }

        /// <summary>
        /// Sample all space fabric properties at a position.
        /// Returns continuous densities and bitmask of active properties.
        /// </summary>
        public SpaceFabricSample SampleFabricAt(Vector2D absolutePosition)
        {
            var cache = GetActiveQueryCache();
            if (cache.TryGetValue("SpaceZones", out var query) && query is SpaceZoneQuery szq)
                return szq.QueryAt(absolutePosition, GetWorldSeed()).FabricSample;
            return default;
        }

        /// <summary>
        /// Sample fabric at a Unity world position (converts via floating origin).
        /// </summary>
        public SpaceFabricSample SampleFabricAtWorldPosition(Vector2 worldPosition)
        {
            if (_worldService == null)
                return default;

            Vector2D absolute = _worldService.WorldToAbsolute(worldPosition);
            return SampleFabricAt(absolute);
        }

        /// <summary>
        /// Query what space zone type exists at a position (legacy).
        /// </summary>
        public SpaceZoneType GetZoneAt(Vector2D absolutePosition)
        {
            var cache = GetActiveQueryCache();
            if (cache.TryGetValue("SpaceZones", out var query) && query is SpaceZoneQuery szq)
                return szq.QueryAt(absolutePosition, GetWorldSeed()).ZoneType;
            return SpaceZoneType.OpenSpace;
        }

        /// <summary>
        /// Query which faction controls a position.
        /// </summary>
        public FactionTerritoryInfo GetFactionAt(Vector2D absolutePosition)
        {
            var cache = GetActiveQueryCache();
            if (cache.TryGetValue("FactionTerritory", out var query) && query is IWorldLayerQuery<FactionTerritoryInfo> ftq)
                return ftq.QueryAt(absolutePosition, GetWorldSeed());
            return FactionTerritoryInfo.Neutral;
        }

        /// <summary>
        /// Sample resource properties at a position (with zone correlation).
        /// </summary>
        public ResourceFabricSample SampleResourcesAt(Vector2D absolutePosition)
        {
            var cache = GetActiveQueryCache();
            if (cache.TryGetValue("Resources", out var query) && query is ResourceQuery rq)
            {
                var zoneSample = SampleFabricAt(absolutePosition);
                return rq.QueryWithZone(absolutePosition, GetWorldSeed(), zoneSample).FabricSample;
            }
            return default;
        }

        /// <summary>
        /// Sample resources at a Unity world position (converts via floating origin).
        /// </summary>
        public ResourceFabricSample SampleResourcesAtWorldPosition(Vector2 worldPosition)
        {
            if (_worldService == null)
                return default;

            Vector2D absolute = _worldService.WorldToAbsolute(worldPosition);
            return SampleResourcesAt(absolute);
        }

        /// <summary>
        /// Query danger level at a position.
        /// </summary>
        public float GetDangerLevelAt(Vector2D absolutePosition)
        {
            float danger = 0f;

            var zone = GetZoneAt(absolutePosition);
            danger += zone switch
            {
                SpaceZoneType.Anomaly => 0.5f,
                SpaceZoneType.NebulaDense => 0.2f,
                _ => 0f
            };

            var faction = GetFactionAt(absolutePosition);
            if (faction.IsContestedZone)
                danger += 0.3f;
            danger += (1f - faction.ControlStrength) * 0.2f;

            return Mathf.Clamp01(danger);
        }

        /// <summary>
        /// Get comprehensive world info at a position.
        /// </summary>
        public WorldLocationInfo GetWorldInfoAt(Vector2D absolutePosition)
        {
            return new WorldLocationInfo
            {
                Position = absolutePosition,
                ZoneType = GetZoneAt(absolutePosition),
                FactionInfo = GetFactionAt(absolutePosition),
                DangerLevel = GetDangerLevelAt(absolutePosition),
                FabricSample = SampleFabricAt(absolutePosition),
                ResourceSample = SampleResourcesAt(absolutePosition),
            };
        }

        /// <summary>
        /// Get world info at a Unity world position.
        /// </summary>
        public WorldLocationInfo GetWorldInfoAtWorldPosition(Vector2 worldPosition)
        {
            if (_worldService == null)
                return WorldLocationInfo.Default;

            Vector2D absolute = _worldService.WorldToAbsolute(worldPosition);
            return GetWorldInfoAt(absolute);
        }

        private float GetWorldSeed()
        {
            return _worldService?.WorldSeed ?? 0f;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmos()
        {
            if (config == null || !config.showDebugGizmos) return;
            if (_worldService == null || _worldService.ChunkManager == null) return;

            foreach (var kvp in _worldService.ChunkManager.LoadedChunks)
            {
                var chunk = kvp.Value;
                DrawChunkLayerGizmos(chunk);
            }
        }

        private void DrawChunkLayerGizmos(Chunk chunk)
        {
            Vector2 worldCenter = _worldService.AbsoluteToWorld(chunk.Coord.ToAbsoluteCenter(_worldService.ChunkSize));

            // Draw zone color
            var zoneData = chunk.GetData<SpaceZoneLayerData>();
            if (zoneData != null)
            {
                Color zoneColor = zoneData.PrimaryZoneType switch
                {
                    SpaceZoneType.DeepVoid => new Color(0.1f, 0.1f, 0.2f, 0.3f),
                    SpaceZoneType.SparseSpace => new Color(0.2f, 0.2f, 0.3f, 0.3f),
                    SpaceZoneType.OpenSpace => new Color(0.3f, 0.3f, 0.4f, 0.3f),
                    SpaceZoneType.AsteroidBelt => new Color(0.5f, 0.4f, 0.3f, 0.3f),
                    SpaceZoneType.NebulaDense => new Color(0.6f, 0.2f, 0.6f, 0.3f),
                    SpaceZoneType.Anomaly => new Color(0.8f, 0.2f, 0.2f, 0.3f),
                    _ => new Color(0.3f, 0.3f, 0.3f, 0.3f)
                };

                Gizmos.color = zoneColor;
                Gizmos.DrawCube(new Vector3(worldCenter.x, worldCenter.y, 0), new Vector3(_worldService.ChunkSize * 0.95f, _worldService.ChunkSize * 0.95f, 0));
            }

            // Draw faction border indicator
            var factionData = chunk.GetData<FactionTerritoryData>();
            if (factionData != null && factionData.IsContestedZone)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(new Vector3(worldCenter.x, worldCenter.y, 0), new Vector3(_worldService.ChunkSize, _worldService.ChunkSize, 0));
            }
        }

        #endregion

        #region Debug Preview

        public enum PreviewMode { SpaceZones, Factions, FactionBorders, Danger, Combined, NebulaDensity, AsteroidDensity, VoidFactor, AnomalyStrength, MineralDensity, OreDensity, GasDensity, ExoticDensity, WaterDensity, ResourceValue }

        [Header("Debug Preview")]
        [SerializeField] private bool enablePreview = false;
        [SerializeField] private PreviewMode previewMode = PreviewMode.Combined;
        [SerializeField] private int previewResolution = 256;
        [SerializeField] private float previewWorldSize = 500000f;
        [SerializeField] private Vector2 previewCenter = Vector2.zero;
        [SerializeField] private bool previewFollowCamera = true;

        [Space]
        [Tooltip("Minimum seconds between preview updates (higher = better FPS)")]
        [Range(0.1f, 2f)]
        [SerializeField] private float previewUpdateInterval = 0.5f;

        [Space]
        [SerializeField] private Texture2D previewTexture;

        private Color32[] _previewPixels;
        private bool _previewDirty = true;
        private float _lastPreviewTime;

        public Texture2D PreviewTexture => previewTexture;

        private void Update()
        {
            if (!enablePreview) return;

            // Update center to follow camera
            if (previewFollowCamera && _worldService != null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector2D absolute = _worldService.WorldToAbsolute(cam.transform.position);
                    previewCenter = new Vector2((float)absolute.X, (float)absolute.Y);
                    _previewDirty = true;
                }
            }

            // Throttle preview updates to avoid tanking FPS
            if (_previewDirty && Time.time - _lastPreviewTime >= previewUpdateInterval)
            {
                GeneratePreview();
                _previewDirty = false;
                _lastPreviewTime = Time.time;
            }
        }

        [ContextMenu("Generate Preview")]
        public void GeneratePreview()
        {
            if (previewTexture == null || previewTexture.width != previewResolution)
            {
                if (previewTexture != null)
                    DestroyImmediate(previewTexture);

                previewTexture = new Texture2D(previewResolution, previewResolution, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            if (_previewPixels == null || _previewPixels.Length != previewResolution * previewResolution)
                _previewPixels = new Color32[previewResolution * previewResolution];

            float halfSize = previewWorldSize * 0.5f;
            float step = previewWorldSize / previewResolution;
            float seed = GetWorldSeed();

            for (int y = 0; y < previewResolution; y++)
            {
                for (int x = 0; x < previewResolution; x++)
                {
                    float worldX = previewCenter.x - halfSize + x * step;
                    float worldY = previewCenter.y - halfSize + y * step;
                    Vector2D pos = new Vector2D(worldX, worldY);

                    Color color = previewMode switch
                    {
                        PreviewMode.SpaceZones => GetZoneColor(pos, seed),
                        PreviewMode.Factions => GetFactionColor(pos, seed),
                        PreviewMode.FactionBorders => GetBorderColor(pos, seed),
                        PreviewMode.Danger => GetDangerColor(pos, seed),
                        PreviewMode.Combined => GetCombinedColor(pos, seed),
                        PreviewMode.NebulaDensity => GetFabricPropertyColor(pos, SpaceProperty.Nebula),
                        PreviewMode.AsteroidDensity => GetFabricPropertyColor(pos, SpaceProperty.Asteroids),
                        PreviewMode.VoidFactor => GetFabricPropertyColor(pos, SpaceProperty.Void),
                        PreviewMode.AnomalyStrength => GetFabricPropertyColor(pos, SpaceProperty.Anomaly),
                        PreviewMode.MineralDensity => GetResourcePropertyColor(pos, ResourceProperty.Mineral),
                        PreviewMode.OreDensity => GetResourcePropertyColor(pos, ResourceProperty.Ore),
                        PreviewMode.GasDensity => GetResourcePropertyColor(pos, ResourceProperty.Gas),
                        PreviewMode.ExoticDensity => GetResourcePropertyColor(pos, ResourceProperty.Exotic),
                        PreviewMode.WaterDensity => GetResourcePropertyColor(pos, ResourceProperty.Water),
                        PreviewMode.ResourceValue => GetResourceValueColor(pos),
                        _ => Color.black
                    };

                    _previewPixels[y * previewResolution + x] = color;
                }
            }

            previewTexture.SetPixels32(_previewPixels);
            previewTexture.Apply();
        }

        private Color GetFabricPropertyColor(Vector2D pos, SpaceProperty property)
        {
            var sample = SampleFabricAt(pos);
            float density = property switch
            {
                SpaceProperty.Nebula => sample.NebulaDensity,
                SpaceProperty.Asteroids => sample.AsteroidDensity,
                SpaceProperty.Void => sample.VoidFactor,
                SpaceProperty.Anomaly => sample.AnomalyStrength,
                _ => 0f
            };

            Color baseColor = property switch
            {
                SpaceProperty.Nebula => new Color(0.6f, 0.2f, 0.6f),
                SpaceProperty.Asteroids => new Color(0.6f, 0.4f, 0.2f),
                SpaceProperty.Void => new Color(0.1f, 0.1f, 0.3f),
                SpaceProperty.Anomaly => new Color(0.8f, 0.2f, 0.2f),
                _ => Color.white
            };

            return Color.Lerp(Color.black, baseColor, density);
        }

        private Color GetResourcePropertyColor(Vector2D pos, ResourceProperty property)
        {
            var sample = SampleResourcesAt(pos);
            float density = property switch
            {
                ResourceProperty.Mineral => sample.MineralDensity,
                ResourceProperty.Ore => sample.OreDensity,
                ResourceProperty.Gas => sample.GasDensity,
                ResourceProperty.Exotic => sample.ExoticDensity,
                ResourceProperty.Water => sample.WaterDensity,
                _ => 0f
            };

            Color baseColor = property switch
            {
                ResourceProperty.Mineral => new Color(0.7f, 0.7f, 0.4f),
                ResourceProperty.Ore => new Color(0.6f, 0.3f, 0.1f),
                ResourceProperty.Gas => new Color(0.3f, 0.7f, 0.3f),
                ResourceProperty.Exotic => new Color(0.8f, 0.2f, 0.8f),
                ResourceProperty.Water => new Color(0.2f, 0.5f, 0.9f),
                _ => Color.white
            };

            return Color.Lerp(Color.black, baseColor, density);
        }

        private Color GetResourceValueColor(Vector2D pos)
        {
            var sample = SampleResourcesAt(pos);
            float value = sample.OverallResourceValue;

            // Gradient: black → green → yellow → red for increasing value
            if (value < 0.5f)
                return Color.Lerp(Color.black, new Color(0.2f, 0.6f, 0.1f), value * 2f);
            else
                return Color.Lerp(new Color(0.2f, 0.6f, 0.1f), new Color(0.9f, 0.7f, 0.1f), (value - 0.5f) * 2f);
        }

        private Color GetZoneColor(Vector2D pos, float seed)
        {
            var zone = GetZoneAt(pos);
            return zone switch
            {
                SpaceZoneType.DeepVoid => new Color(0.05f, 0.05f, 0.1f),
                SpaceZoneType.SparseSpace => new Color(0.1f, 0.1f, 0.2f),
                SpaceZoneType.OpenSpace => new Color(0.2f, 0.2f, 0.3f),
                SpaceZoneType.AsteroidBelt => new Color(0.4f, 0.3f, 0.2f),
                SpaceZoneType.NebulaDense => new Color(0.5f, 0.2f, 0.5f),
                SpaceZoneType.Anomaly => new Color(0.8f, 0.1f, 0.1f),
                _ => Color.gray
            };
        }

        private Color GetFactionColor(Vector2D pos, float seed)
        {
            var faction = GetFactionAt(pos);

            if (faction.ControllingFaction.IsNeutral)
                return new Color(0.2f, 0.2f, 0.2f);

            if (faction.ControllingFaction.IsMinor)
            {
                // Generate color from cell ID for minor factions
                int hash = faction.ControllingFaction.Value.GetHashCode();
                float h = (hash & 0xFF) / 255f;
                return Color.HSVToRGB(h, 0.4f, 0.5f);
            }

            // Major faction - try to get color from config
            if (config?.factionConfig?.majorFactions != null)
            {
                foreach (var f in config.factionConfig.majorFactions)
                {
                    if (f.factionId == faction.ControllingFaction.Value)
                        return f.primaryColor;
                }
            }

            // Fallback - generate from ID
            int h2 = faction.ControllingFaction.Value.GetHashCode();
            return Color.HSVToRGB((h2 & 0xFF) / 255f, 0.7f, 0.8f);
        }

        private Color GetBorderColor(Vector2D pos, float seed)
        {
            var faction = GetFactionAt(pos);

            if (faction.IsContestedZone)
                return Color.red;

            float borderFade = Mathf.InverseLerp(0, config?.factionConfig?.contestedDensityGap ?? 0.15f, faction.DistanceToBorder);
            return Color.Lerp(Color.yellow, Color.black, borderFade);
        }

        private Color GetDangerColor(Vector2D pos, float seed)
        {
            float danger = GetDangerLevelAt(pos);
            return Color.Lerp(Color.green, Color.red, danger);
        }

        private Color GetCombinedColor(Vector2D pos, float seed)
        {
            var zone = GetZoneAt(pos);
            var faction = GetFactionAt(pos);

            // Base color from faction
            Color baseColor = GetFactionColor(pos, seed);

            // Darken based on zone type
            float zoneMult = zone switch
            {
                SpaceZoneType.DeepVoid => 0.3f,
                SpaceZoneType.SparseSpace => 0.5f,
                SpaceZoneType.NebulaDense => 0.7f,
                _ => 1f
            };
            baseColor *= zoneMult;

            // Add red tint for contested zones
            if (faction.IsContestedZone)
                baseColor = Color.Lerp(baseColor, Color.red, 0.3f);

            // Overlay zone-specific effects
            if (zone == SpaceZoneType.AsteroidBelt)
                baseColor = Color.Lerp(baseColor, new Color(0.6f, 0.4f, 0.2f), 0.4f);
            else if (zone == SpaceZoneType.NebulaDense)
                baseColor = Color.Lerp(baseColor, new Color(0.6f, 0.2f, 0.6f), 0.3f);
            else if (zone == SpaceZoneType.Anomaly)
                baseColor = Color.Lerp(baseColor, Color.red, 0.5f);

            return baseColor;
        }

        public void MarkPreviewDirty()
        {
            _previewDirty = true;
        }

        #endregion
    }

    /// <summary>
    /// Comprehensive world information at a position.
    /// </summary>
    public struct WorldLocationInfo
    {
        public Vector2D Position;
        public SpaceZoneType ZoneType;
        public FactionTerritoryInfo FactionInfo;
        public float DangerLevel;
        public SpaceFabricSample FabricSample;
        public ResourceFabricSample ResourceSample;

        public static WorldLocationInfo Default => new()
        {
            ZoneType = SpaceZoneType.OpenSpace,
            FactionInfo = FactionTerritoryInfo.Neutral,
            DangerLevel = 0f,
        };
    }
}
