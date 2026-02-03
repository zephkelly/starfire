using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using StarfireV2;
using StarfireV2.Pooling;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Creates visual representations for celestial bodies using pooled prefab instances.
    /// Falls back to procedural creation if no prefabs are configured.
    /// </summary>
    public class CelestialBodyConsumer : IChunkDataConsumer
    {
        private readonly CelestialBodyConfig _config;
        private readonly List<CelestialBodyChunkData> _activeChunkData = new List<CelestialBodyChunkData>();
        private Transform _containerParent;

        public Type DataType => typeof(CelestialBodyChunkData);

        public CelestialBodyConsumer(CelestialBodyConfig config)
        {
            _config = config;
        }

        public void OnChunkLoaded(Chunk.Chunk chunk)
        {
            var data = chunk.GetData<CelestialBodyChunkData>();
            if (data == null || data.Bodies.Count == 0)
                return;

            EnsureContainer();

            Vector2 chunkCenterAbsolute = chunk.GetAbsoluteCenter();
            Vector2 worldChunkCenter = GetWorldPosition(chunkCenterAbsolute);

            foreach (var definition in data.Bodies)
            {
                Vector2 worldPos = worldChunkCenter + definition.LocalPosition;
                var result = CreateBodyVisual(definition.Info, worldPos);
                if (result.instance != null)
                    data.RuntimeObjects.Add(result);
            }

            _activeChunkData.Add(data);
        }

        public void OnChunkUnloading(Chunk.Chunk chunk)
        {
            var data = chunk.GetData<CelestialBodyChunkData>();
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
            // Future: LOD transitions, parallax updates based on camera distance
        }

        private (GameObject instance, GameObject prefab) CreateBodyVisual(CelestialBodyInfo info, Vector2 worldPosition)
        {
            GameObject prefab = GetPrefab(info);
            GameObject go;

            if (prefab != null && WorldObjectPoolManager.Instance != null)
            {
                go = WorldObjectPoolManager.Instance.Get(prefab);
            }
            else
            {
                go = CreateProceduralBody(info);
                prefab = null;
            }

            go.transform.position = new Vector3(worldPosition.x, worldPosition.y, GetSortingZ(info));

            if (info.Kind == CelestialBodyKind.Star)
                go.transform.localScale = Vector3.one * (info.Radius * 0.01f);
            else
                go.transform.localScale = Vector3.one * (info.Radius * 0.01f);

            if (_containerParent != null)
                go.transform.SetParent(_containerParent, true);

            return (go, prefab);
        }

        private GameObject GetPrefab(CelestialBodyInfo info)
        {
            if (_config == null) return null;

            if (info.Kind == CelestialBodyKind.Star)
            {
                foreach (var starConfig in _config.starTypes)
                {
                    if (starConfig.type == info.StarType)
                        return starConfig.prefab;
                }
            }
            else
            {
                foreach (var planetConfig in _config.planetTypes)
                {
                    if (planetConfig.type == info.PlanetType)
                    {
                        if (info.HasRing && planetConfig.ringedPrefab != null)
                            return planetConfig.ringedPrefab;
                        return planetConfig.prefab;
                    }
                }
            }

            return null;
        }

        private GameObject CreateProceduralBody(CelestialBodyInfo info)
        {
            var go = new GameObject(info.Kind == CelestialBodyKind.Star
                ? $"Star_{info.StarType}_{info.Seed:F0}"
                : $"Planet_{info.PlanetType}_{info.Seed:F0}");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = info.Kind == CelestialBodyKind.Star ? -100 : -90;

            if (info.Kind == CelestialBodyKind.Star)
                sr.color = GetStarColor(info.StarType);
            else
                sr.color = GetPlanetColor(info.PlanetType);

            if (info.HasRing)
            {
                var ringGo = new GameObject("Ring");
                ringGo.transform.SetParent(go.transform, false);
                var ringSr = ringGo.AddComponent<SpriteRenderer>();
                ringSr.color = new Color(0.6f, 0.5f, 0.3f, 0.4f);
                ringSr.sortingOrder = sr.sortingOrder - 1;
                float ringScale = info.RingOuterRadius / Mathf.Max(info.Radius, 1f);
                ringGo.transform.localScale = Vector3.one * ringScale;
            }

            return go;
        }

        private float GetSortingZ(CelestialBodyInfo info)
        {
            if (info.Kind == CelestialBodyKind.Star) return 10f;
            return 5f + info.ParallaxDepthFactor * 5f;
        }

        private Color GetStarColor(StarType type)
        {
            return type switch
            {
                StarType.RedDwarf => new Color(1f, 0.4f, 0.3f),
                StarType.YellowStar => new Color(1f, 0.95f, 0.7f),
                StarType.BlueGiant => new Color(0.6f, 0.7f, 1f),
                StarType.WhiteDwarf => new Color(0.9f, 0.9f, 1f),
                StarType.Neutron => new Color(0.8f, 0.9f, 1f),
                _ => Color.white
            };
        }

        private Color GetPlanetColor(PlanetType type)
        {
            return type switch
            {
                PlanetType.Rocky => new Color(0.6f, 0.5f, 0.4f),
                PlanetType.GasGiant => new Color(0.8f, 0.6f, 0.4f),
                PlanetType.IceGiant => new Color(0.5f, 0.7f, 0.9f),
                PlanetType.Molten => new Color(0.9f, 0.3f, 0.1f),
                PlanetType.Barren => new Color(0.5f, 0.5f, 0.5f),
                _ => Color.gray
            };
        }

        private void EnsureContainer()
        {
            if (_containerParent != null) return;
            var containerGo = new GameObject("[CelestialBodies]");
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
