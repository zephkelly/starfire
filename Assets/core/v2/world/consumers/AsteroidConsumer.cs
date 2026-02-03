using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.V2.World.Generation.Generators;
using Starfire.Core.V2.World.Simulation;
using StarfireV2;
using StarfireV2.Pooling;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Creates visual representations for asteroids using pooled prefab instances.
    /// Falls back to procedural creation if no prefabs are configured.
    /// On chunk unload, registers moving asteroids with BackgroundSimulationManager.
    /// On chunk load, spawns simulated asteroids at their current positions.
    /// </summary>
    public class AsteroidConsumer : IChunkDataConsumer
    {
        private readonly AsteroidGenerationConfig _config;
        private readonly List<AsteroidChunkData> _activeChunkData = new List<AsteroidChunkData>();
        private Transform _containerParent;

        public Type DataType => typeof(AsteroidChunkData);

        public AsteroidConsumer(AsteroidGenerationConfig config)
        {
            _config = config;
        }

        public void OnChunkLoaded(Chunk.Chunk chunk)
        {
            var data = chunk.GetData<AsteroidChunkData>();
            if (data == null)
                return;

            EnsureContainer();

            Vector2 chunkCenterAbsolute = chunk.GetAbsoluteCenter();
            Vector2 worldChunkCenter = GetWorldPosition(chunkCenterAbsolute);

            // Check for simulated entities that should be spawned in this chunk
            var simManager = BackgroundSimulationManager.Instance;
            List<SimulatedEntity> simulatedEntities = null;
            if (simManager != null)
            {
                simulatedEntities = simManager.PromoteEntitiesForChunk(chunk.Coord);
            }

            // Spawn procedural asteroids
            if (data.Asteroids.Count > 0)
            {
                foreach (var definition in data.Asteroids)
                {
                    Vector2 worldPos = worldChunkCenter + definition.LocalPosition;
                    var result = CreateAsteroidVisual(definition, worldPos);
                    if (result.instance != null)
                        data.RuntimeObjects.Add(result);
                }
            }

            // Spawn simulated asteroids at their predicted positions
            if (simulatedEntities != null && simulatedEntities.Count > 0)
            {
                var service = WorldGenerationService.Instance;
                foreach (var simEntity in simulatedEntities)
                {
                    if (simEntity.EntityType != EntityType.Asteroid) continue;

                    Vector2 worldPos = service != null
                        ? service.AbsoluteToWorld(simEntity.AbsolutePosition)
                        : simEntity.AbsolutePosition.ToVector2();

                    var def = new AsteroidDefinition
                    {
                        LocalPosition = Vector2.zero, // Not chunk-relative anymore
                        Size = simEntity.Radius * 2f,
                        Rotation = simEntity.Rotation,
                        Variant = simEntity.Variant,
                        Seed = simEntity.Seed,
                        Source = (AsteroidSource)simEntity.SourceType
                    };

                    var result = CreateAsteroidVisual(def, worldPos);
                    if (result.instance != null)
                    {
                        // Apply velocity to the Rigidbody2D so it continues moving
                        var rb = result.instance.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            rb.linearVelocity = simEntity.Velocity.ToVector2();
                            rb.angularVelocity = simEntity.AngularVelocity;
                        }

                        data.RuntimeObjects.Add(result);
                    }
                }
            }

            _activeChunkData.Add(data);
        }

        public void OnChunkUnloading(Chunk.Chunk chunk)
        {
            var data = chunk.GetData<AsteroidChunkData>();
            if (data == null) return;

            var simManager = BackgroundSimulationManager.Instance;
            var service = WorldGenerationService.Instance;
            var poolManager = WorldObjectPoolManager.Instance;

            Debug.Log($"[AsteroidConsumer] OnChunkUnloading: Chunk=({chunk.Coord.X}, {chunk.Coord.Y}), RuntimeObjects={data.RuntimeObjects.Count}, SimManager={(simManager != null ? "EXISTS" : "NULL")}, Service={(service != null ? "EXISTS" : "NULL")}");

            float velocityThreshold = simManager?.Config?.minVelocitySqrToSimulate ?? 0.01f;
            Debug.Log($"[AsteroidConsumer] Velocity threshold (sqr): {velocityThreshold} (linear: {Mathf.Sqrt(velocityThreshold):F3})");

            for (int i = 0; i < data.RuntimeObjects.Count; i++)
            {
                var (instance, prefab) = data.RuntimeObjects[i];
                if (instance == null)
                {
                    Debug.Log($"[AsteroidConsumer]   [{i}] instance is NULL - skipping");
                    continue;
                }

                // Check if this asteroid has velocity — if so, register with simulation
                if (simManager != null && service != null)
                {
                    var rb = instance.GetComponent<Rigidbody2D>();
                    if (rb == null)
                    {
                        Debug.Log($"[AsteroidConsumer]   [{i}] NO Rigidbody2D on {instance.name}");
                    }
                    else
                    {
                        float velSqr = rb.linearVelocity.sqrMagnitude;
                        Debug.Log($"[AsteroidConsumer]   [{i}] {instance.name}: velocity=({rb.linearVelocity.x:F2}, {rb.linearVelocity.y:F2}), velSqr={velSqr:F4}, threshold={velocityThreshold}, passes={velSqr > velocityThreshold}");

                        if (velSqr > velocityThreshold)
                        {
                            var absolutePos = service.WorldToAbsolute(instance.transform.position);

                            // Determine visual properties from the definition if available
                            int variant = 0;
                            float seed = 0f;
                            int sourceType = 0;
                            float size = instance.transform.localScale.x;

                            if (i < data.Asteroids.Count)
                            {
                                var def = data.Asteroids[i];
                                variant = def.Variant;
                                seed = def.Seed;
                                sourceType = (int)def.Source;
                                size = def.Size;
                            }

                            var simEntity = new SimulatedEntity
                            {
                                EntityId = GenerateAsteroidId(chunk.Coord, i),
                                EntityType = EntityType.Asteroid,
                                CurrentChunk = chunk.Coord,
                                OriginChunk = chunk.Coord,
                                AbsolutePosition = absolutePos,
                                Velocity = Vector2D.FromVector2(rb.linearVelocity),
                                Rotation = rb.rotation,
                                AngularVelocity = rb.angularVelocity,
                                Mass = rb.mass,
                                Radius = size * 0.5f,
                                Drag = rb.linearDamping,
                                HasBeenModified = true,
                                Variant = variant,
                                Seed = seed,
                                SourceType = sourceType
                            };

                            Debug.Log($"[AsteroidConsumer]   [{i}] REGISTERING entity ID={simEntity.EntityId} with BackgroundSimulationManager");
                            simManager.RegisterEntity(simEntity);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[AsteroidConsumer]   [{i}] Cannot check velocity - simManager={(simManager != null ? "OK" : "NULL")}, service={(service != null ? "OK" : "NULL")}");
                }

                // Return to pool
                if (prefab != null && poolManager != null)
                    poolManager.Return(instance, prefab);
                else
                    UnityEngine.Object.Destroy(instance);
            }

            data.RuntimeObjects.Clear();
            _activeChunkData.Remove(data);
        }

        public void OnOriginShift(Vector2 offset)
        {
            foreach (var data in _activeChunkData)
            {
                foreach (var (instance, _) in data.RuntimeObjects)
                {
                    if (instance != null)
                        instance.transform.position += (Vector3)offset;
                }
            }
        }

        public void Update(float deltaTime)
        {
            // Future: LOD transitions, entity promotion for nearby asteroids
        }

        private (GameObject instance, GameObject prefab) CreateAsteroidVisual(AsteroidDefinition def, Vector2 worldPosition)
        {
            GameObject prefab = GetPrefab(def);
            GameObject go;

            if (prefab != null && WorldObjectPoolManager.Instance != null)
            {
                go = WorldObjectPoolManager.Instance.Get(prefab);
            }
            else
            {
                // Fallback: procedural creation when no prefab assigned
                go = CreateProceduralAsteroid(def);
                prefab = null;
            }

            go.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
            go.transform.rotation = Quaternion.Euler(0, 0, def.Rotation);
            go.transform.localScale = Vector3.one * def.Size;

            if (_containerParent != null)
                go.transform.SetParent(_containerParent, true);

            return (go, prefab);
        }

        private GameObject GetPrefab(AsteroidDefinition def)
        {
            if (_config == null || _config.variantPrefabs == null || _config.variantPrefabs.Length == 0)
                return null;

            int index = def.Variant % _config.variantPrefabs.Length;
            return _config.variantPrefabs[index];
        }

        private GameObject CreateProceduralAsteroid(AsteroidDefinition def)
        {
            var go = new GameObject($"Asteroid_{def.Source}_{def.Seed:F0}");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -50;
            sr.color = def.Source switch
            {
                AsteroidSource.Belt => new Color(0.5f, 0.4f, 0.3f),
                AsteroidSource.PlanetaryRing => new Color(0.6f, 0.5f, 0.35f),
                AsteroidSource.Scatter => new Color(0.4f, 0.4f, 0.4f),
                _ => Color.gray
            };
            return go;
        }

        private void EnsureContainer()
        {
            if (_containerParent != null) return;
            var containerGo = new GameObject("[Asteroids]");
            _containerParent = containerGo.transform;
        }

        private Vector2 GetWorldPosition(Vector2 absolutePosition)
        {
            var service = WorldGenerationService.Instance;
            if (service != null)
                return service.AbsoluteToWorld(absolutePosition);
            return absolutePosition;
        }

        /// <summary>
        /// Generate a deterministic unique ID for an asteroid based on its chunk and index.
        /// </summary>
        private static int GenerateAsteroidId(ChunkCoord coord, int index)
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash ^ coord.X.GetHashCode()) * 16777619;
                hash = (hash ^ coord.Y.GetHashCode()) * 16777619;
                hash = (hash ^ index) * 16777619;
                return hash;
            }
        }
    }
}
