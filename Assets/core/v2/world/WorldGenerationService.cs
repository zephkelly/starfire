using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.V2.World.Generation;
using Starfire.Core.V2.World.Generation.Generators;
using Starfire.Core.V2.World.Consumers;
using Starfire.Core.V2.Cam;
using Starfire.Core.Background.Regions;
using Starfire.Core.V2.World.Simulation;
using StarfireV2;

namespace Starfire.Core.V2.World
{
    /// <summary>
    /// Main service orchestrating chunk-based world generation.
    /// Manages chunk lifecycle, generators, consumers, and floating origin.
    /// </summary>
    [ExecuteAlways]
    public class WorldGenerationService : MonoBehaviour
    {
        public static WorldGenerationService Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private WorldGenerationConfig config;

        [Header("Background Simulation")]
        [SerializeField] private BackgroundSimulationConfig simulationConfig;

        [Header("Camera Reference")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool autoFindCamera = true;

        // Core components
        private ChunkManager _chunkManager;
        private readonly List<IChunkDataGenerator> _generators = new List<IChunkDataGenerator>();
        private readonly List<IChunkDataConsumer> _consumers = new List<IChunkDataConsumer>();

        // Floating origin tracking
        private Vector2D _absolutePosition;
        private Vector2D _originOffset;
        private Vector2 _lastCameraPosition;

        // Events
        public event Action<Chunk.Chunk> OnChunkGenerated;
        public event Action<Chunk.Chunk> OnChunkDestroyed;
        public event Action<Vector2> OnOriginShift;

        /// <summary>
        /// Current origin offset. Add to absolute positions to get Unity positions.
        /// </summary>
        public Vector2D OriginOffset => _originOffset;

        /// <summary>
        /// Current absolute position of the camera (in infinite world space).
        /// </summary>
        public Vector2D AbsolutePosition => _absolutePosition;

        /// <summary>
        /// Current world seed.
        /// </summary>
        public float WorldSeed => config != null ? config.worldSeed : 0f;

        /// <summary>
        /// Chunk size in world units.
        /// </summary>
        public float ChunkSize => config != null ? config.chunkSize : 500f;

        /// <summary>
        /// Floating origin reset distance threshold.
        /// </summary>
        public float FloatingOriginLimit => config != null ? config.floatingOriginLimit : 2560f;

        /// <summary>
        /// Get the chunk manager for direct access.
        /// </summary>
        public ChunkManager ChunkManager => _chunkManager;

        /// <summary>
        /// Whether the service is initialized and running.
        /// </summary>
        public bool IsInitialized => _chunkManager != null;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple WorldGenerationService instances detected. Destroying duplicate.");
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            Instance = this;
            Initialize();
        }

        private void OnDisable()
        {
            Cleanup();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (config == null || _chunkManager == null) return;

            UpdateCameraReference();
            if (targetCamera == null) return;

            UpdateAbsolutePosition();
            CheckFloatingOriginReset();
            UpdateChunks();
            UpdateConsumers();
        }

        private void OnDrawGizmos()
        {
            if (config == null || !config.enableDebugGizmos) return;
            if (_chunkManager == null) return;

            DrawChunkDebugGizmos();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (config == null)
            {
                Debug.LogWarning("WorldGenerationService: No config assigned.");
                return;
            }

            // Initialize seed if random
            float seed = config.worldSeed;
            if (seed == 0f)
            {
                seed = UnityEngine.Random.Range(1f, 100000f);
                config.worldSeed = seed;
            }

            // Create chunk manager
            _chunkManager = new ChunkManager(config.chunkSize, seed)
            {
                LoadRadius = config.loadRadius,
                UnloadRadius = config.unloadRadius,
                MaxLoadedChunks = config.maxLoadedChunks,
                ChunksPerFrame = config.chunksPerFrame,
                MaxUnloadsPerFrame = config.maxUnloadsPerFrame
            };

            // Subscribe to chunk events
            _chunkManager.OnChunkLoading += HandleChunkLoading;
            _chunkManager.OnChunkLoaded += HandleChunkLoaded;
            _chunkManager.OnChunkUnloading += HandleChunkUnloading;
            _chunkManager.OnChunkUnloaded += HandleChunkUnloaded;

            // Initialize position tracking
            _absolutePosition = Vector2D.Zero;
            _originOffset = Vector2D.Zero;

            UpdateCameraReference();
            if (targetCamera != null)
            {
                _lastCameraPosition = targetCamera.transform.position;
                _absolutePosition = Vector2D.FromVector2(_lastCameraPosition);
            }

            // Create built-in generators
            InitializeGenerators();

            // Create built-in consumers
            InitializeConsumers();

            // Initialize background simulation
            InitializeSimulation();
        }

        private void Cleanup()
        {
            if (_chunkManager != null)
            {
                _chunkManager.OnChunkLoading -= HandleChunkLoading;
                _chunkManager.OnChunkLoaded -= HandleChunkLoaded;
                _chunkManager.OnChunkUnloading -= HandleChunkUnloading;
                _chunkManager.OnChunkUnloaded -= HandleChunkUnloaded;

                _chunkManager.UnloadAllChunks();
                _chunkManager = null;
            }

            _generators.Clear();
            _consumers.Clear();
        }

        private void InitializeGenerators()
        {
            // Nebula generation - prefer fabric-integrated when available
            if (config.useFabricNebulaGeneration && config.fabricNebulaConfig != null)
            {
                // Use fabric-integrated generator (queries SpaceZoneLayer data)
                var fabricNebulaGenerator = new FabricNebulaChunkGenerator(config.fabricNebulaConfig);
                RegisterGenerator(fabricNebulaGenerator);
            }
            else if (config.nebulaConfig != null)
            {
                // Fall back to legacy independent generator
                var nebulaGenerator = new NebulaChunkGenerator(config.nebulaConfig);
                RegisterGenerator(nebulaGenerator);
            }

            // Celestial body generation
            if (config.celestialBodyConfig != null && config.celestialBodyConfig.enabled)
            {
                var celestialGenerator = new CelestialBodyChunkGenerator(config.celestialBodyConfig);
                RegisterGenerator(celestialGenerator);
            }

            // Asteroid generation
            if (config.asteroidConfig != null && config.asteroidConfig.enabled)
            {
                var asteroidGenerator = new AsteroidChunkGenerator(config.asteroidConfig);
                RegisterGenerator(asteroidGenerator);
            }

            // Sort generators by priority
            _generators.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        private void InitializeConsumers()
        {
            // Add nebula consumer
            var nebulaConsumer = new NebulaRegionConsumer();
            RegisterConsumer(nebulaConsumer);

            // Add celestial body consumer
            if (config.celestialBodyConfig != null && config.celestialBodyConfig.enabled)
            {
                var celestialConsumer = new CelestialBodyConsumer(config.celestialBodyConfig.fabricConfig);
                RegisterConsumer(celestialConsumer);
            }

            // Add asteroid consumer
            if (config.asteroidConfig != null && config.asteroidConfig.enabled)
            {
                var asteroidConsumer = new AsteroidConsumer(config.asteroidConfig);
                RegisterConsumer(asteroidConsumer);
            }
        }

        private void InitializeSimulation()
        {
            Debug.Log($"[WorldGenService] InitializeSimulation called. simulationConfig={(simulationConfig != null ? simulationConfig.name : "NULL")}");

            if (simulationConfig == null)
            {
                Debug.LogWarning("[WorldGenService] simulationConfig is NULL - BackgroundSimulationManager will NOT be created!");
                return;
            }

            // Don't create if one already exists
            if (BackgroundSimulationManager.Instance != null)
            {
                Debug.Log("[WorldGenService] BackgroundSimulationManager.Instance already exists - skipping creation");
                return;
            }

            var go = new GameObject("[BackgroundSimulation]");
            go.transform.SetParent(transform);
            var simManager = go.AddComponent<BackgroundSimulationManager>();
            simManager.Initialize(simulationConfig, config.chunkSize);
            Debug.Log($"[WorldGenService] BackgroundSimulationManager created and initialized. ChunkSize={config.chunkSize}");
        }

        private void UpdateCameraReference()
        {
            if (targetCamera == null && autoFindCamera)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindFirstObjectByType<Camera>();
                }
            }
        }

        #endregion

        #region Position and Origin

        private void UpdateAbsolutePosition()
        {
            Vector2 currentPos = targetCamera.transform.position;
            Vector2 delta = currentPos - _lastCameraPosition;
            _lastCameraPosition = currentPos;

            _absolutePosition += Vector2D.FromVector2(delta);
        }

        private void CheckFloatingOriginReset()
        {
            Vector2 cameraPos = targetCamera.transform.position;
            float distanceFromOrigin = cameraPos.magnitude;

            if (distanceFromOrigin > config.floatingOriginLimit)
            {
                PerformOriginShift();
            }
        }

        private void PerformOriginShift()
        {
            Vector2 cameraPos = targetCamera.transform.position;

            // Get the current chunk center in Unity space
            ChunkCoord currentChunk = ChunkCoord.FromAbsolutePosition(_absolutePosition, config.chunkSize);
            Vector2D absoluteChunkCenter = currentChunk.ToAbsoluteCenter(config.chunkSize);
            Vector2 unityChunkCenter = AbsoluteToWorld(absoluteChunkCenter);

            // Calculate offset to keep player near chunk center
            Vector2 playerOffsetFromChunkCenter = cameraPos - unityChunkCenter;

            // The shift amount (negate camera position, keep chunk-relative offset)
            Vector2 shiftAmount = -cameraPos + playerOffsetFromChunkCenter;

            // Update origin offset BEFORE moving objects
            _originOffset += Vector2D.FromVector2(-shiftAmount);

            // Move all registered entities (uses Rigidbody2D.position for physics objects)
            ShiftAllEntities(shiftAmount);

            // Move the camera
            targetCamera.transform.position += (Vector3)shiftAmount;

            // Notify camera controller to update its internal tracking state
            var cameraController = targetCamera.GetComponent<VelocityCameraController>();
            cameraController?.OnOriginShift(shiftAmount);

            // Update tracking to match new camera position
            _lastCameraPosition = targetCamera.transform.position;

            // Notify all consumers (nebulas, etc.)
            foreach (var consumer in _consumers)
            {
                consumer.OnOriginShift(shiftAmount);
            }

            // Fire event for external systems
            OnOriginShift?.Invoke(shiftAmount);
        }

        private void ShiftAllEntities(Vector2 shiftAmount)
        {
            var registry = EntityRegistry.Instance;
            if (registry == null) return;

            foreach (var entity in registry.AllEntities)
            {
                if (entity == null) continue;

                // Shift both Rigidbody2D.position AND Transform.position to ensure immediate sync
                // Note: Setting Rigidbody2D.position in LateUpdate doesn't update Transform until next physics step
                if (entity is ShipController shipController && shipController.Rigid2D != null)
                {
                    shipController.Rigid2D.position += shiftAmount;
                    shipController.Transform.position += (Vector3)shiftAmount;
                }
                else if (entity.Transform != null)
                {
                    entity.Transform.position += (Vector3)shiftAmount;
                }
            }
        }

        /// <summary>
        /// Convert an absolute position to Unity world position.
        /// </summary>
        public Vector2 AbsoluteToWorld(Vector2D absolutePos)
        {
            return (absolutePos - _originOffset).ToVector2();
        }

        /// <summary>
        /// Convert a Unity world position to absolute position.
        /// </summary>
        public Vector2D WorldToAbsolute(Vector2 worldPos)
        {
            return Vector2D.FromVector2(worldPos) + _originOffset;
        }

        #endregion

        #region Chunk Updates

        private void UpdateChunks()
        {
            _chunkManager.UpdateAroundPosition(_absolutePosition);
        }

        private void UpdateConsumers()
        {
            float deltaTime = Time.deltaTime;
            foreach (var consumer in _consumers)
            {
                consumer.Update(deltaTime);
            }
        }

        private void HandleChunkLoading(Chunk.Chunk chunk)
        {
   
        }

        private void HandleChunkLoaded(Chunk.Chunk chunk)
        {
            // Run generators
            var context = new ChunkGenerationContext(
                _chunkManager.WorldSeed,
                chunk,
                _chunkManager.ChunkSize,
                _originOffset
            );

            foreach (var generator in _generators)
            {
                if (!generator.IsEnabled) continue;

                try
                {
                    generator.Generate(chunk, context);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Generator {generator.GetType().Name} failed for chunk {chunk.Coord}: {e}");
                }
            }

            // Notify consumers
            foreach (var consumer in _consumers)
            {
                if (chunk.HasData(consumer.DataType))
                {
                    try
                    {
                        consumer.OnChunkLoaded(chunk);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Consumer {consumer.GetType().Name} failed for chunk {chunk.Coord}: {e}");
                    }
                }
            }

            OnChunkGenerated?.Invoke(chunk);
        }

        private void HandleChunkUnloading(Chunk.Chunk chunk)
        {
            // Notify generators
            foreach (var generator in _generators)
            {
                if (chunk.HasData(generator.DataType))
                {
                    try
                    {
                        generator.OnChunkUnloading(chunk);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Generator {generator.GetType().Name} cleanup failed for chunk {chunk.Coord}: {e}");
                    }
                }
            }

            // Notify consumers
            foreach (var consumer in _consumers)
            {
                if (chunk.HasData(consumer.DataType))
                {
                    try
                    {
                        consumer.OnChunkUnloading(chunk);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Consumer {consumer.GetType().Name} cleanup failed for chunk {chunk.Coord}: {e}");
                    }
                }
            }
        }

        private void HandleChunkUnloaded(Chunk.Chunk chunk)
        {
            OnChunkDestroyed?.Invoke(chunk);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Register a content generator.
        /// </summary>
        public void RegisterGenerator(IChunkDataGenerator generator)
        {
            if (generator == null) return;
            if (_generators.Contains(generator)) return;

            _generators.Add(generator);
            _generators.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        /// <summary>
        /// Unregister a content generator.
        /// </summary>
        public void UnregisterGenerator(IChunkDataGenerator generator)
        {
            _generators.Remove(generator);
        }

        /// <summary>
        /// Register a data consumer.
        /// </summary>
        public void RegisterConsumer(IChunkDataConsumer consumer)
        {
            if (consumer == null) return;
            if (_consumers.Contains(consumer)) return;

            _consumers.Add(consumer);
        }

        /// <summary>
        /// Unregister a data consumer.
        /// </summary>
        public void UnregisterConsumer(IChunkDataConsumer consumer)
        {
            _consumers.Remove(consumer);
        }

        /// <summary>
        /// Get chunk at an absolute position.
        /// </summary>
        public Chunk.Chunk GetChunkAtAbsolutePosition(Vector2D absolutePos)
        {
            return _chunkManager?.GetChunkAtAbsolutePosition(absolutePos);
        }

        /// <summary>
        /// Get chunk at a Unity world position.
        /// </summary>
        public Chunk.Chunk GetChunkAtWorldPosition(Vector2 worldPos)
        {
            Vector2D absolutePos = WorldToAbsolute(worldPos);
            return GetChunkAtAbsolutePosition(absolutePos);
        }

        /// <summary>
        /// Force regeneration of a specific chunk.
        /// </summary>
        public void RegenerateChunk(ChunkCoord coord)
        {
            if (_chunkManager == null) return;

            _chunkManager.ForceUnloadChunk(coord);
            _chunkManager.ForceLoadChunk(coord);
        }

        /// <summary>
        /// Reset entire world with optional new seed.
        /// </summary>
        public void ResetWorld(float? newSeed = null)
        {
            Cleanup();

            if (newSeed.HasValue && config != null)
            {
                config.worldSeed = newSeed.Value;
            }

            Initialize();
        }

        /// <summary>
        /// Manually trigger an origin shift.
        /// External systems should call this after shifting all game objects.
        /// </summary>
        /// <param name="shiftAmount">The amount everything was shifted by</param>
        public void NotifyOriginShift(Vector2 shiftAmount)
        {
            _originOffset += Vector2D.FromVector2(-shiftAmount);
            _lastCameraPosition += shiftAmount;

            foreach (var consumer in _consumers)
            {
                consumer.OnOriginShift(shiftAmount);
            }

            OnOriginShift?.Invoke(shiftAmount);
        }

        #endregion

        #region Debug

        private void DrawChunkDebugGizmos()
        {
            if (_chunkManager == null) return;

            int nebulaChunkCount = 0;
            int totalRegions = 0;

            foreach (var kvp in _chunkManager.LoadedChunks)
            {
                var chunk = kvp.Value;
                Vector2 worldCenter = AbsoluteToWorld(chunk.Coord.ToAbsoluteCenter(config.chunkSize));
                Vector3 center = new Vector3(worldCenter.x, worldCenter.y, 0);
                Vector3 size = new Vector3(config.chunkSize, config.chunkSize, 0);

                // Check for nebula data
                var nebulaData = chunk.GetData<NebulaChunkData>();
                bool hasNebula = nebulaData != null && nebulaData.Regions.Count > 0;

                if (hasNebula)
                {
                    nebulaChunkCount++;
                    totalRegions += nebulaData.Regions.Count;
                }

                // Draw chunk bounds with appropriate color
                Color color;
                if (hasNebula)
                {
                    color = config.nebulaChunkColor;
                }
                else
                {
                    color = chunk.State == ChunkState.Loaded
                        ? config.loadedChunkColor
                        : config.loadingChunkColor;
                }

                Gizmos.color = color;
                Gizmos.DrawWireCube(center, size);

                // Draw filled quad
                Gizmos.color = new Color(color.r, color.g, color.b, color.a * 0.3f);
                Gizmos.DrawCube(center, size);

                // Draw nebula region boundaries if present
                if (hasNebula)
                {
                    DrawNebulaRegionGizmos(nebulaData, worldCenter);
                }
            }

            // Draw center marker
            if (targetCamera != null)
            {
                Gizmos.color = Color.white;
                Vector3 camPos = targetCamera.transform.position;
                Gizmos.DrawLine(camPos - Vector3.right * 10, camPos + Vector3.right * 10);
                Gizmos.DrawLine(camPos - Vector3.up * 10, camPos + Vector3.up * 10);
            }

            // Debug: Draw text showing nebula stats near origin
            #if UNITY_EDITOR
            if (nebulaChunkCount > 0 || totalRegions > 0)
            {
                Gizmos.color = Color.magenta;
                // Draw a marker at origin showing we have nebula data
                Gizmos.DrawWireSphere(Vector3.zero, 5f);
            }
            #endif
        }

        private void DrawNebulaRegionGizmos(NebulaChunkData data, Vector2 chunkWorldCenter)
        {
            foreach (var definition in data.Regions)
            {
                Vector2 regionCenter = chunkWorldCenter + definition.LocalPosition;
                Vector3 center3D = new Vector3(regionCenter.x, regionCenter.y, 0);

                // Draw center marker
                Gizmos.color = Color.white;
                float markerSize = 5f;
                Gizmos.DrawLine(center3D - Vector3.right * markerSize, center3D + Vector3.right * markerSize);
                Gizmos.DrawLine(center3D - Vector3.up * markerSize, center3D + Vector3.up * markerSize);

                // Draw boundaries based on edge behavior
                var regionConfig = definition.Config;
                if (regionConfig == null) continue;

                switch (regionConfig.edgeBehavior)
                {
                    case NebulaEdgeBehavior.SmoothFalloff:
                        // Outer edge (where fade ends)
                        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
                        DrawCircleGizmo(center3D, definition.Radius, 64);
                        // Inner edge (where fade starts)
                        float innerRadius = Mathf.Max(0f, definition.Radius - regionConfig.falloffDistance);
                        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
                        DrawCircleGizmo(center3D, innerRadius, 64);
                        break;

                    case NebulaEdgeBehavior.SharpBoundary:
                        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
                        DrawCircleGizmo(center3D, definition.Radius, 64);
                        break;

                    case NebulaEdgeBehavior.InverseFalloff:
                        // Inner edge (clear zone boundary)
                        Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.8f);
                        DrawCircleGizmo(center3D, definition.Radius, 64);
                        // Outer edge (full nebula density)
                        float outerRadius = definition.Radius + regionConfig.falloffDistance;
                        Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.4f);
                        DrawCircleGizmo(center3D, outerRadius, 64);
                        break;
                }
            }
        }

        private void DrawCircleGizmo(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }

        #endregion
    }
}
