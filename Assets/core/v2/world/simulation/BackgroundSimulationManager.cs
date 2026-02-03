using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.Save;
using Starfire.Core.V2.Save.Tracking;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

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
        private ChunkCoord _lastPlayerChunk;
        private bool _initialized;

        public BackgroundSimulationConfig Config => config;
        public Tier1ActiveSimulator Tier1 => _tier1;
        public Tier2BallisticTracker Tier2 => _tier2;
        public Texture2D PreviewTexture => previewTexture;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Initialize with a config. Called by WorldGenerationService after chunk manager is ready.
        /// </summary>
        public void Initialize(BackgroundSimulationConfig cfg, double chunkSize)
        {
            Debug.Log($"[BackgroundSim] Initialize called. Config={cfg?.name ?? "NULL"}, ChunkSize={chunkSize}");

            config = cfg;
            _tier1 = new Tier1ActiveSimulator(config, chunkSize);
            _tier2 = new Tier2BallisticTracker(config);

            _tier1.OnEntityMigratedChunk += HandleEntityMigratedChunk;

            _initialized = true;
            Debug.Log($"[BackgroundSim] Initialization complete. _initialized={_initialized}, Tier1={_tier1 != null}, Tier2={_tier2 != null}");
        }

        private void Update()
        {
            if (!_initialized || !Application.isPlaying) return;

            _tier1.Tick(Time.deltaTime);

            // Check tier transitions when player crosses chunk boundary
            var worldGen = WorldGenerationService.Instance;
            if (worldGen == null) return;

            var playerChunk = ChunkCoord.FromAbsolutePosition(worldGen.AbsolutePosition, worldGen.ChunkSize);
            if (playerChunk != _lastPlayerChunk)
            {
                ProcessTierTransitions(playerChunk);
                _lastPlayerChunk = playerChunk;
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
        /// Clear all simulated entities (called on load).
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
            // Draw Tier 1 entities (cyan)
            if (_tier1 != null)
            {
                foreach (var entity in _tier1.GetAllEntities())
                {
                    DrawEntityDot(entity.AbsolutePosition, halfSize, step, new Color(0f, 1f, 1f), 3);
                }
            }

            // Draw Tier 2 entities (orange)
            if (_tier2 != null)
            {
                double now = Time.timeAsDouble;
                foreach (var snapshot in _tier2.GetAllSnapshots())
                {
                    var predicted = _tier2.PredictEntity(snapshot.EntityId, now);
                    if (predicted != null)
                    {
                        DrawEntityDot(predicted.AbsolutePosition, halfSize, step, new Color(1f, 0.6f, 0f), 3);
                    }
                }
            }
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
    }
}
