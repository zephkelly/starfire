using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.V2.World.Generation.Generators;
using StarfireV2.Pooling;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Creates visual representations for asteroids using pooled prefab instances.
    /// Falls back to procedural creation if no prefabs are configured.
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
            if (data == null || data.Asteroids.Count == 0)
                return;

            EnsureContainer();

            Vector2 chunkCenterAbsolute = chunk.GetAbsoluteCenter();
            Vector2 worldChunkCenter = GetWorldPosition(chunkCenterAbsolute);

            foreach (var definition in data.Asteroids)
            {
                Vector2 worldPos = worldChunkCenter + definition.LocalPosition;
                var result = CreateAsteroidVisual(definition, worldPos);
                if (result.instance != null)
                    data.RuntimeObjects.Add(result);
            }

            _activeChunkData.Add(data);
        }

        public void OnChunkUnloading(Chunk.Chunk chunk)
        {
            var data = chunk.GetData<AsteroidChunkData>();
            if (data == null) return;

            var poolManager = WorldObjectPoolManager.Instance;
            foreach (var (instance, prefab) in data.RuntimeObjects)
            {
                if (instance == null) continue;

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
    }
}
