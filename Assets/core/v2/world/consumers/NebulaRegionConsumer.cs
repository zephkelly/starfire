using System;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.Background.Regions;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Bridges chunk nebula data to the existing NebulaRegionManager.
    /// Creates and destroys NebulaRegion instances as chunks load/unload.
    /// </summary>
    public class NebulaRegionConsumer : IChunkDataConsumer
    {
        private NebulaRegionManager _regionManager;
        private static readonly int SeedPropertyID = Shader.PropertyToID("_Seed");

        public Type DataType => typeof(NebulaChunkData);

        public NebulaRegionConsumer()
        {
            // Manager will be resolved lazily
        }

        public void OnChunkLoaded(Chunk.Chunk chunk)
        {
            var data = chunk.GetData<NebulaChunkData>();
            if (data == null || data.Regions.Count == 0)
                return;

            // Ensure we have the manager
            if (!TryGetRegionManager())
                return;

            // Get chunk center in current Unity world space
            Vector2 absoluteChunkCenter = chunk.GetAbsoluteCenter();
            Vector2 worldChunkCenter = GetWorldPosition(absoluteChunkCenter);

            foreach (var definition in data.Regions)
            {
                // Calculate world position from chunk-local position
                Vector2 worldPosition = worldChunkCenter + definition.LocalPosition;

                // Create region via NebulaRegionManager
                var region = _regionManager.CreateRegion(
                    worldPosition,
                    definition.Radius,
                    definition.Config
                );

                if (region != null)
                {
                    // Apply unique seed to the material for varied patterns
                    if (region.Material != null)
                    {
                        region.Material.SetFloat(SeedPropertyID, definition.Seed);
                    }

                    data.RuntimeRegions.Add(region);
                }
            }
        }

        public void OnChunkUnloading(Chunk.Chunk chunk)
        {
            var data = chunk.GetData<NebulaChunkData>();
            if (data == null)
                return;

            if (!TryGetRegionManager())
                return;

            // Destroy all runtime regions for this chunk
            foreach (var region in data.RuntimeRegions)
            {
                if (region != null)
                {
                    _regionManager.DestroyRegion(region);
                }
            }

            data.RuntimeRegions.Clear();
        }

        public void OnOriginShift(Vector2 offset)
        {
            // The NebulaRegionManager's quads follow the camera automatically,
            // so we just need to update the WorldPosition values on each region.
            // The manager handles the actual quad positioning in its LateUpdate.

            if (!TryGetRegionManager())
                return;

            var allRegions = _regionManager.GetAllRegions();
            foreach (var region in allRegions)
            {
                // Shift the world position by the offset
                region.WorldPosition += offset;
                region.MarkDirty();
            }
        }

        public void Update(float deltaTime)
        {
            // No per-frame updates needed - NebulaRegionManager handles everything
        }

        private bool TryGetRegionManager()
        {
            if (_regionManager != null)
                return true;

            _regionManager = NebulaRegionManager.Instance;
            return _regionManager != null;
        }

        private Vector2 GetWorldPosition(Vector2 absolutePosition)
        {
            var service = WorldGenerationService.Instance;
            if (service != null)
            {
                return service.AbsoluteToWorld(absolutePosition);
            }

            // Fallback - assume no offset
            return absolutePosition;
        }
    }
}
