using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.Save;
using Starfire.Core.V2.Save.Tracking;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Consumers;
using Starfire.Core.V2.World.Simulation.Behaviors;
using Starfire.Core.V2.World.Simulation.Config;
using Starfire.Core.V2.World.Simulation.Events;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Preview visualization modes for the simulation debug view.
    /// </summary>
    public enum SimulationPreviewMode
    {
        Zones,      // Loaded, Tier1, Tier2 zones as colored regions
        Entities,   // Just the entities as dots on dark background
        Combined,   // Both zones and entities
        ChunkGrid   // Grid overlay showing chunk boundaries with zones
    }

    /// <summary>
    /// Orchestrates multi-tier background simulation for entities outside loaded chunks.
    /// Manages tier transitions based on distance from the player's current chunk.
    /// </summary>
    public class BackgroundSimulationManager : MonoBehaviour
    {
        public static BackgroundSimulationManager Instance { get; private set; }

        [SerializeField] private BackgroundSimulationConfig config;

        [Header("Debug Preview")]
        [SerializeField] private bool enablePreview = false;
        [SerializeField] private SimulationPreviewMode previewMode = SimulationPreviewMode.Combined;
        [SerializeField] private int previewResolution = 256;
        [SerializeField] private float previewWorldSize = 10000f;
        [SerializeField] private bool previewFollowPlayer = true;
        [SerializeField] private Vector2 previewCenter = Vector2.zero;
        [Range(0.1f, 2f)]
        [SerializeField] private float previewUpdateInterval = 0.25f;
        [SerializeField] private Texture2D previewTexture;

        // Preview state
        private Color32[] _previewPixels;
        private bool _previewDirty = true;
        private float _lastPreviewTime;

        private Tier1ActiveSimulator _tier1;
        private Tier2BallisticTracker _tier2;
        private SimulationEventLog _eventLog;
        private SimulationEntityTypeRegistry _registry;
        private ChunkCoord _lastPlayerChunk;
        private bool _initialized;
        private float _lastPruneTime;

        [Header("Behavior System")]
        [Tooltip("Enable the data-driven behavior system. If false, uses legacy physics.")]
        [SerializeField] private bool enableBehaviorSystem = true;

        [Tooltip("Path within Resources folder to load entity type configs from.")]
        [SerializeField] private string entityTypeConfigPath = "Simulation/EntityTypes";

        public BackgroundSimulationConfig Config => config;
        public Tier1ActiveSimulator Tier1 => _tier1;
        public Tier2BallisticTracker Tier2 => _tier2;
        public Texture2D PreviewTexture => previewTexture;

        /// <summary>The entity type registry (available after initialization).</summary>
        public SimulationEntityTypeRegistry Registry => _registry;

        /// <summary>Whether the behavior system is enabled and initialized.</summary>
        public bool BehaviorSystemActive => _tier1?.UseBehaviorSystem ?? false;

        /// <summary>Event log containing collision and destruction events.</summary>
        public SimulationEventLog EventLog => _eventLog;

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
            // Self-initialize if config is assigned via inspector and not already initialized
            if (!_initialized && config != null)
            {
                TryInitialize();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Attempt to initialize using inspector-assigned config and WorldGenerationService chunk size.
        /// </summary>
        private void TryInitialize()
        {
            if (_initialized) return;
            if (config == null)
            {
                Debug.LogWarning("[BackgroundSim] Cannot initialize - config not assigned in inspector!");
                return;
            }

            var worldGen = WorldGenerationService.Instance;
            if (worldGen == null || !worldGen.IsInitialized)
            {
                Debug.LogWarning("[BackgroundSim] Cannot initialize - WorldGenerationService not ready. Will retry next frame.");
                return;
            }

            double chunkSize = worldGen.ChunkSize;
            Debug.Log($"[BackgroundSim] Self-initializing. Config={config.name}, ChunkSize={chunkSize}, BehaviorSystem={enableBehaviorSystem}");

            _tier1 = new Tier1ActiveSimulator(config, chunkSize);
            _tier2 = new Tier2BallisticTracker(config);
            _eventLog = new SimulationEventLog(config.maxEvents, config.eventRetentionSeconds);

            // Initialize the behavior system if enabled
            if (enableBehaviorSystem)
            {
                InitializeBehaviorSystem();
            }

            _tier1.OnEntityMigratedChunk += HandleEntityMigratedChunk;

            // Wire collision and destruction events to the event log
            _tier1.OnCollision += evt =>
            {
                _eventLog.RecordEvent(evt);
                _previewDirty = true;
            };
            _tier1.OnDestruction += evt =>
            {
                _eventLog.RecordEvent(evt);
                _previewDirty = true;
            };

            _initialized = true;
            Debug.Log($"[BackgroundSim] Initialization complete. _initialized={_initialized}, Tier1={_tier1 != null}, Tier2={_tier2 != null}, EventLog={_eventLog != null}, BehaviorSystem={BehaviorSystemActive}");
        }

        /// <summary>
        /// Initialize the data-driven behavior system.
        /// Loads entity type configs from Resources and sets up the registry.
        /// </summary>
        private void InitializeBehaviorSystem()
        {
            _registry = new SimulationEntityTypeRegistry();

            // Try to initialize from the configured path
            _registry.Initialize(entityTypeConfigPath);

            // If no configs were found, that's okay - the system will use legacy physics
            if (_registry.Count == 0)
            {
                Debug.Log($"[BackgroundSim] No entity type configs found in Resources/{entityTypeConfigPath}. " +
                          "Behavior system will use legacy physics for all entities. " +
                          "Create SimulationEntityTypeConfig assets to enable data-driven behaviors.");
            }
            else
            {
                // Pass the registry to Tier 1 simulator
                _tier1.InitializeBehaviorSystem(_registry);
                Debug.Log($"[BackgroundSim] Behavior system initialized with {_registry.Count} entity type configs.");
            }
        }

        /// <summary>
        /// Manually register an entity type config at runtime.
        /// Useful for dynamically adding new entity types.
        /// </summary>
        public void RegisterEntityTypeConfig(SimulationEntityTypeConfig config)
        {
            if (_registry == null)
            {
                Debug.LogWarning("[BackgroundSim] Cannot register config - behavior system not initialized.");
                return;
            }

            _registry.RegisterConfig(config);

            // Re-initialize the behavior system on tier 1 if not already done
            if (!_tier1.UseBehaviorSystem)
            {
                _tier1.InitializeBehaviorSystem(_registry);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            // Retry initialization if not yet initialized (WorldGenerationService may not have been ready)
            if (!_initialized)
            {
                TryInitialize();
                if (!_initialized) return;
            }

            double currentTime = Time.timeAsDouble;

            // Update context with player state if using behavior system
            var worldGen = WorldGenerationService.Instance;
            if (worldGen != null && _tier1?.Context != null)
            {
                var playerChunk = ChunkCoord.FromAbsolutePosition(worldGen.AbsolutePosition, worldGen.ChunkSize);
                _tier1.Context.UpdatePlayerState(playerChunk, worldGen.AbsolutePosition);
            }

            _tier1.Tick(Time.deltaTime, currentTime);

            // Prune old events periodically (every 10 seconds)
            if (Time.time - _lastPruneTime > 10f)
            {
                _eventLog?.Prune(currentTime);
                _lastPruneTime = Time.time;
            }

            // Check tier transitions when player crosses chunk boundary
            if (worldGen == null) return;

            var currentPlayerChunk = ChunkCoord.FromAbsolutePosition(worldGen.AbsolutePosition, worldGen.ChunkSize);
            if (currentPlayerChunk != _lastPlayerChunk)
            {
                ProcessTierTransitions(currentPlayerChunk);
                _lastPlayerChunk = currentPlayerChunk;
                _previewDirty = true;
            }

            // Update preview if enabled and dirty
            if (enablePreview && _previewDirty && Time.time - _lastPreviewTime >= previewUpdateInterval)
            {
                if (previewFollowPlayer)
                {
                    previewCenter = new Vector2((float)worldGen.AbsolutePosition.X, (float)worldGen.AbsolutePosition.Y);
                }
                GeneratePreview();
                _previewDirty = false;
                _lastPreviewTime = Time.time;
            }
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Register an entity for background simulation (enters Tier 1).
        /// </summary>
        public void RegisterEntity(SimulatedEntity entity)
        {
            Debug.Log($"[BackgroundSim] RegisterEntity called. EntityId={entity.EntityId}, Type={entity.EntityType}, Pos=({entity.AbsolutePosition.X:F1}, {entity.AbsolutePosition.Y:F1}), Vel=({entity.Velocity.X:F2}, {entity.Velocity.Y:F2}), _initialized={_initialized}");

            if (!_initialized)
            {
                Debug.LogWarning("[BackgroundSim] RegisterEntity FAILED - not initialized!");
                return;
            }
            entity.LastSimulationTime = Time.timeAsDouble;
            _tier1.AddEntity(entity);
            Debug.Log($"[BackgroundSim] Entity {entity.EntityId} added to Tier1. Total Tier1 count: {_tier1.EntityCount}");
        }

        /// <summary>
        /// Remove an entity from all tiers (e.g., when it's promoted back to a real GameObject).
        /// </summary>
        public void UnregisterEntity(int entityId)
        {
            _tier1?.RemoveEntity(entityId);
            _tier2?.RemoveEntity(entityId);
        }

        /// <summary>
        /// Get all entities in a specific chunk across all tiers.
        /// Tier 2 entities are predicted to their current position.
        /// </summary>
        public List<SimulatedEntity> GetEntitiesInChunk(ChunkCoord coord)
        {
            var result = new List<SimulatedEntity>();

            // Tier 1: already tracked by chunk
            if (_tier1 != null)
            {
                result.AddRange(_tier1.GetEntitiesInChunk(coord));
            }

            // Tier 2: predict and check chunk
            if (_tier2 != null)
            {
                double now = Time.timeAsDouble;
                double chunkSize = WorldGenerationService.Instance?.ChunkSize ?? 500.0;

                foreach (var snapshot in _tier2.GetAllSnapshots())
                {
                    var predictedChunk = _tier2.PredictChunk(snapshot.EntityId, now, chunkSize);
                    if (predictedChunk.HasValue && predictedChunk.Value == coord)
                    {
                        var predicted = _tier2.PredictEntity(snapshot.EntityId, now);
                        if (predicted != null)
                            result.Add(predicted);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get all simulated entities across all tiers (Tier 2 entities are predicted).
        /// </summary>
        public IEnumerable<SimulatedEntity> GetAllSimulatedEntities()
        {
            if (_tier1 != null)
            {
                foreach (var entity in _tier1.GetAllEntities())
                    yield return entity;
            }

            if (_tier2 != null)
            {
                double now = Time.timeAsDouble;
                foreach (var snapshot in _tier2.GetAllSnapshots())
                {
                    var predicted = _tier2.PredictEntity(snapshot.EntityId, now);
                    if (predicted != null)
                        yield return predicted;
                }
            }
        }

        /// <summary>
        /// Called when a chunk is about to load. Returns any simulated entities that belong
        /// to that chunk so they can be spawned as real GameObjects instead of procedural ones.
        /// Removes them from the simulation.
        /// </summary>
        public List<SimulatedEntity> PromoteEntitiesForChunk(ChunkCoord coord)
        {
            var entities = GetEntitiesInChunk(coord);

            // Remove promoted entities from their respective tiers
            foreach (var entity in entities)
            {
                UnregisterEntity(entity.EntityId);
            }

            return entities;
        }

        /// <summary>
        /// Handle origin shift by offsetting all Tier 1 entities.
        /// Tier 2 uses absolute coordinates and is unaffected.
        /// </summary>
        public void OnOriginShift(Vector2 shiftAmount)
        {
            // Tier 1 entities use absolute coordinates, so no shift needed.
            // Tier 2 snapshots use absolute coordinates, so no shift needed.
            // Nothing to do — this is the benefit of using Vector2D throughout.
        }

        // ── Tier Transitions ────────────────────────────────────────────

        private void ProcessTierTransitions(ChunkCoord playerChunk)
        {
            if (config == null) return;

            double chunkSize = WorldGenerationService.Instance?.ChunkSize ?? 500.0;
            double now = Time.timeAsDouble;

            // Demote Tier 1 → Tier 2 (entities too far from player)
            var toDemote = new List<int>();
            foreach (var entity in _tier1.GetAllEntities())
            {
                long dist = entity.CurrentChunk.ChebyshevDistance(playerChunk);
                if (dist > config.tier2StartDistance)
                {
                    toDemote.Add(entity.EntityId);
                }
            }
            foreach (int id in toDemote)
            {
                var entity = _tier1.RemoveEntity(id);
                if (entity != null)
                {
                    entity.LastSimulationTime = now;
                    _tier2.AddEntity(entity, now);
                }
            }

            // Promote Tier 2 → Tier 1 (entities that moved back into active range)
            var toPromote = new List<int>();
            foreach (var snapshot in _tier2.GetAllSnapshots())
            {
                var predictedChunk = _tier2.PredictChunk(snapshot.EntityId, now, chunkSize);
                if (predictedChunk.HasValue)
                {
                    long dist = predictedChunk.Value.ChebyshevDistance(playerChunk);
                    if (dist <= config.tier2StartDistance)
                    {
                        toPromote.Add(snapshot.EntityId);
                    }
                }
            }
            foreach (int id in toPromote)
            {
                var entity = _tier2.PredictEntity(id, now);
                if (entity != null)
                {
                    _tier2.RemoveEntity(id);
                    _tier1.AddEntity(entity);
                }
            }
        }

        // ── Event Handlers ──────────────────────────────────────────────

        private void HandleEntityMigratedChunk(SimulatedEntity entity, ChunkCoord from, ChunkCoord to)
        {
            var tracker = SaveSystem.Instance?.ChunkTracker;
            if (tracker == null) return;

            tracker.OnEntityMigrated(entity.EntityId, from, to);
        }

        // ── Save/Load Support ───────────────────────────────────────────

        /// <summary>
        /// Restore a simulated entity from save data. Places it in the appropriate tier.
        /// </summary>
        public void RestoreEntity(SimulatedEntity entity, ChunkCoord playerChunk)
        {
            long dist = entity.CurrentChunk.ChebyshevDistance(playerChunk);

            if (dist <= config.tier2StartDistance)
            {
                _tier1.AddEntity(entity);
            }
            else
            {
                _tier2.AddEntity(entity, entity.LastSimulationTime);
            }
        }

        /// <summary>
        /// Clear all simulated entities and events (called on load).
        /// </summary>
        public void Clear()
        {
            // Remove all from tier 1
            var tier1Ids = new List<int>();
            if (_tier1 != null)
            {
                foreach (var e in _tier1.GetAllEntities())
                    tier1Ids.Add(e.EntityId);
                foreach (int id in tier1Ids)
                    _tier1.RemoveEntity(id);
            }

            _tier2?.Clear();
            _eventLog?.Clear();
        }

        // ── Debug Preview ──────────────────────────────────────────────────

        /// <summary>
        /// Mark the preview as needing regeneration.
        /// </summary>
        public void MarkPreviewDirty() => _previewDirty = true;

        /// <summary>
        /// Generate the preview texture based on current simulation state.
        /// </summary>
        [ContextMenu("Generate Preview")]
        public void GeneratePreview()
        {
            var worldGen = WorldGenerationService.Instance;
            if (worldGen == null) return;

            // Create or resize texture if needed
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
            double chunkSize = worldGen.ChunkSize;
            int loadRadius = worldGen.ChunkManager?.LoadRadius ?? 3;

            var playerChunk = ChunkCoord.FromAbsolutePosition(worldGen.AbsolutePosition, chunkSize);

            // Fill pixels based on mode
            for (int y = 0; y < previewResolution; y++)
            {
                for (int x = 0; x < previewResolution; x++)
                {
                    float worldX = previewCenter.x - halfSize + x * step;
                    float worldY = previewCenter.y - halfSize + y * step;
                    var absolutePos = new Vector2D(worldX, worldY);
                    var sampleChunk = ChunkCoord.FromAbsolutePosition(absolutePos, chunkSize);

                    Color color = previewMode switch
                    {
                        SimulationPreviewMode.Zones => GetZoneColor(playerChunk, sampleChunk, loadRadius, chunkSize, worldX, worldY, false),
                        SimulationPreviewMode.Entities => new Color(0.05f, 0.05f, 0.08f),
                        SimulationPreviewMode.Combined => GetZoneColor(playerChunk, sampleChunk, loadRadius, chunkSize, worldX, worldY, false),
                        SimulationPreviewMode.ChunkGrid => GetZoneColor(playerChunk, sampleChunk, loadRadius, chunkSize, worldX, worldY, true),
                        _ => Color.black
                    };

                    _previewPixels[y * previewResolution + x] = color;
                }
            }

            // Draw entities on top
            if (previewMode == SimulationPreviewMode.Entities ||
                previewMode == SimulationPreviewMode.Combined ||
                previewMode == SimulationPreviewMode.ChunkGrid)
            {
                DrawEntities(halfSize, step);
            }

            // Draw player crosshair
            DrawCrosshair(halfSize, step);

            previewTexture.SetPixels32(_previewPixels);
            previewTexture.Apply();
        }

        private Color GetZoneColor(ChunkCoord playerChunk, ChunkCoord sampleChunk, int loadRadius, double chunkSize, float worldX, float worldY, bool drawGrid)
        {
            long distance = sampleChunk.ChebyshevDistance(playerChunk);

            // Determine base color based on zone
            Color baseColor;
            if (distance <= loadRadius)
            {
                baseColor = new Color(0.2f, 0.3f, 0.5f); // Blue - loaded
            }
            else if (config != null && distance <= loadRadius + config.tier1ChunkRadius)
            {
                baseColor = new Color(0.15f, 0.4f, 0.2f); // Green - Tier 1
            }
            else if (config != null && distance <= config.tier2StartDistance)
            {
                baseColor = new Color(0.4f, 0.35f, 0.15f); // Orange - Tier 2
            }
            else
            {
                baseColor = new Color(0.08f, 0.05f, 0.12f); // Dark purple - beyond
            }

            // Draw chunk grid lines if enabled
            if (drawGrid)
            {
                double chunkLocalX = worldX - sampleChunk.X * chunkSize;
                double chunkLocalY = worldY - sampleChunk.Y * chunkSize;
                float gridThreshold = (float)(chunkSize * 0.02); // 2% of chunk size

                if (chunkLocalX < gridThreshold || chunkLocalX > chunkSize - gridThreshold ||
                    chunkLocalY < gridThreshold || chunkLocalY > chunkSize - gridThreshold)
                {
                    baseColor = Color.Lerp(baseColor, Color.white, 0.3f);
                }
            }

            return baseColor;
        }

        private void DrawEntities(float halfSize, float step)
        {
            // Calculate a scale factor to keep entities visible when zoomed out
            // Uses logarithmic scaling capped at 3x to prevent markers from becoming too large
            float scaleFactor = Mathf.Clamp(1f + Mathf.Log10(Mathf.Max(1f, previewWorldSize / 5000f)), 1f, 3f);

            var worldGen = WorldGenerationService.Instance;

            // Draw loaded GameObjects (entities in loaded chunks)
            DrawLoadedEntities(halfSize, step, scaleFactor, worldGen);

            // Draw Tier 1 entities
            if (_tier1 != null)
            {
                foreach (var entity in _tier1.GetAllEntities())
                {
                    var (color, baseRadius) = GetEntityVisual(entity.EntityType, true);
                    int radius = Mathf.Max(1, Mathf.RoundToInt(baseRadius * scaleFactor));
                    DrawEntityDot(entity.AbsolutePosition, halfSize, step, color, radius);
                }
            }

            // Draw Tier 2 entities
            if (_tier2 != null)
            {
                double now = Time.timeAsDouble;
                foreach (var snapshot in _tier2.GetAllSnapshots())
                {
                    var predicted = _tier2.PredictEntity(snapshot.EntityId, now);
                    if (predicted != null)
                    {
                        var (color, baseRadius) = GetEntityVisual(predicted.EntityType, false);
                        int radius = Mathf.Max(1, Mathf.RoundToInt(baseRadius * scaleFactor));
                        DrawEntityDot(predicted.AbsolutePosition, halfSize, step, color, radius);
                    }
                }
            }
        }

        /// <summary>
        /// Draw entities that exist as loaded GameObjects in the scene.
        /// </summary>
        private void DrawLoadedEntities(float halfSize, float step, float scaleFactor, WorldGenerationService worldGen)
        {
            if (worldGen == null) return;

            // Find all entity trackers in the scene (loaded GameObjects)
            var trackers = FindObjectsOfType<RuntimeEntityTrackerBase>();

            foreach (var tracker in trackers)
            {
                if (tracker == null || tracker.transform == null) continue;

                // Convert world position to absolute position
                var absolutePos = worldGen.WorldToAbsolute(tracker.transform.position);

                // Get visual with "loaded" brightness boost
                var (color, baseRadius) = GetEntityVisual(tracker.EntityType, true);

                // Make loaded entities brighter/more saturated to distinguish them
                color = Color.Lerp(color, Color.white, 0.3f);

                int radius = Mathf.Max(1, Mathf.RoundToInt(baseRadius * scaleFactor));
                DrawEntityDot(absolutePos, halfSize, step, color, radius);
            }
        }

        /// <summary>
        /// Get the visual properties (color, radius) for an entity type and tier.
        /// </summary>
        private (Color color, int radius) GetEntityVisual(EntityType entityType, bool isTier1)
        {
            // Base colors by entity type
            Color baseColor = entityType switch
            {
                EntityType.Asteroid => new Color(0.6f, 0.4f, 0.2f),   // Brown/tan for asteroids
                EntityType.Ship => new Color(0f, 0.8f, 1f),           // Cyan for ships
                EntityType.Station => new Color(0.8f, 0.2f, 0.8f),    // Magenta for stations
                EntityType.Projectile => new Color(1f, 0.2f, 0.2f),   // Red for projectiles
                _ => new Color(0.5f, 0.5f, 0.5f)                      // Gray for unknown
            };

            // Tier 2 entities are slightly dimmer to show they're predicted
            if (!isTier1)
            {
                baseColor = Color.Lerp(baseColor, Color.black, 0.3f);
            }

            // Size by entity type
            int radius = entityType switch
            {
                EntityType.Asteroid => 2,
                EntityType.Ship => 4,
                EntityType.Station => 5,
                EntityType.Projectile => 1,
                _ => 2
            };

            return (baseColor, radius);
        }

        private void DrawEntityDot(Vector2D absolutePos, float halfSize, float step, Color color, int radius)
        {
            float relX = (float)(absolutePos.X - previewCenter.x + halfSize);
            float relY = (float)(absolutePos.Y - previewCenter.y + halfSize);

            int pixelX = Mathf.RoundToInt(relX / step);
            int pixelY = Mathf.RoundToInt(relY / step);

            // Draw filled circle
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        int px = pixelX + dx;
                        int py = pixelY + dy;
                        if (px >= 0 && px < previewResolution && py >= 0 && py < previewResolution)
                        {
                            _previewPixels[py * previewResolution + px] = color;
                        }
                    }
                }
            }
        }

        private void DrawCrosshair(float halfSize, float step)
        {
            var worldGen = WorldGenerationService.Instance;
            if (worldGen == null) return;

            float relX = (float)(worldGen.AbsolutePosition.X - previewCenter.x + halfSize);
            float relY = (float)(worldGen.AbsolutePosition.Y - previewCenter.y + halfSize);

            int centerX = Mathf.RoundToInt(relX / step);
            int centerY = Mathf.RoundToInt(relY / step);

            Color crosshairColor = Color.white;
            int armLength = 5;

            // Horizontal arm
            for (int dx = -armLength; dx <= armLength; dx++)
            {
                int px = centerX + dx;
                if (px >= 0 && px < previewResolution && centerY >= 0 && centerY < previewResolution)
                {
                    _previewPixels[centerY * previewResolution + px] = crosshairColor;
                }
            }

            // Vertical arm
            for (int dy = -armLength; dy <= armLength; dy++)
            {
                int py = centerY + dy;
                if (centerX >= 0 && centerX < previewResolution && py >= 0 && py < previewResolution)
                {
                    _previewPixels[py * previewResolution + centerX] = crosshairColor;
                }
            }
        }

        /// <summary>
        /// Get counts of entities in each tier for display.
        /// </summary>
        public (int tier1Count, int tier2Count) GetEntityCounts()
        {
            int tier1 = _tier1?.EntityCount ?? 0;
            int tier2 = _tier2?.SnapshotCount ?? 0;
            return (tier1, tier2);
        }

        /// <summary>
        /// Get entity counts broken down by type.
        /// Returns counts for asteroids, ships, stations, projectiles, and other.
        /// </summary>
        public (int asteroids, int ships, int stations, int projectiles, int other) GetEntityCountsByType()
        {
            int asteroids = 0, ships = 0, stations = 0, projectiles = 0, other = 0;

            // Count Tier 1 entities
            if (_tier1 != null)
            {
                foreach (var entity in _tier1.GetAllEntities())
                {
                    switch (entity.EntityType)
                    {
                        case EntityType.Asteroid: asteroids++; break;
                        case EntityType.Ship: ships++; break;
                        case EntityType.Station: stations++; break;
                        case EntityType.Projectile: projectiles++; break;
                        default: other++; break;
                    }
                }
            }

            // Count Tier 2 entities
            if (_tier2 != null)
            {
                foreach (var snapshot in _tier2.GetAllSnapshots())
                {
                    switch (snapshot.EntityType)
                    {
                        case EntityType.Asteroid: asteroids++; break;
                        case EntityType.Ship: ships++; break;
                        case EntityType.Station: stations++; break;
                        case EntityType.Projectile: projectiles++; break;
                        default: other++; break;
                    }
                }
            }

            return (asteroids, ships, stations, projectiles, other);
        }

        /// <summary>
        /// Get count of loaded entities (GameObjects with RuntimeEntityTrackerBase).
        /// </summary>
        public int GetLoadedEntityCount()
        {
            var trackers = FindObjectsOfType<RuntimeEntityTrackerBase>();
            return trackers?.Length ?? 0;
        }

        /// <summary>
        /// Get event statistics for display.
        /// </summary>
        public (int totalEvents, int collisions, int destructions) GetEventStats()
        {
            if (_eventLog == null)
                return (0, 0, 0);

            return (_eventLog.EventCount, _eventLog.CollisionCount, _eventLog.DestructionCount);
        }

        /// <summary>
        /// Get collision statistics from last frame.
        /// </summary>
        public (int collisions, int destructions) GetLastFrameStats()
        {
            if (_tier1 == null)
                return (0, 0);

            return (_tier1.LastFrameCollisionCount, _tier1.LastFrameDestructionCount);
        }
    }
}
