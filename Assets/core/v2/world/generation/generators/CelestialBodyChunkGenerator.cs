using System;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using StarfireV2;

namespace Starfire.Core.V2.World.Generation.Generators
{
    /// <summary>
    /// Generates celestial body data for chunks by querying the CelestialBodyLayer.
    /// Finds all stars and planets whose influence radius overlaps the chunk.
    /// </summary>
    public class CelestialBodyChunkGenerator : IChunkDataGenerator
    {
        private readonly CelestialBodyGenerationConfig _config;
        private CelestialBodyQuery _celestialQuery;
        private float _cachedWorldSeed;

        public int Priority => 5;
        public bool IsEnabled => _config != null && _config.enabled;
        public Type DataType => typeof(CelestialBodyChunkData);

        public CelestialBodyChunkGenerator(CelestialBodyGenerationConfig config)
        {
            _config = config;
        }

        public void Generate(Chunk.Chunk chunk, ChunkGenerationContext context)
        {
            var data = new CelestialBodyChunkData();

            EnsureQuery(context.WorldSeed);
            if (_celestialQuery == null)
            {
                chunk.SetData(data);
                return;
            }

            Vector2D chunkCenter = chunk.Coord.ToAbsoluteCenter(context.ChunkSize);
            float searchRadius = context.ChunkSize * 0.5f + _config.searchMargin;

            var bodies = _celestialQuery.GetBodiesInRadius(chunkCenter, searchRadius, context.WorldSeed);

            Vector2 chunkCenterV2 = chunk.GetAbsoluteCenter();

            foreach (var body in bodies)
            {
                Vector2 bodyWorldPos = new Vector2((float)body.AbsolutePosition.X, (float)body.AbsolutePosition.Y);
                Vector2 localPosition = bodyWorldPos - chunkCenterV2;

                data.Bodies.Add(new CelestialBodyDefinition
                {
                    LocalPosition = localPosition,
                    Info = body,
                });
            }

            chunk.SetData(data);
        }

        public void OnChunkUnloading(Chunk.Chunk chunk)
        {
            // Consumer handles runtime cleanup
        }

        private void EnsureQuery(float worldSeed)
        {
            if (_celestialQuery != null && Mathf.Approximately(_cachedWorldSeed, worldSeed))
                return;

            var fabricService = WorldFabricService.Instance;
            if (fabricService == null)
            {
                _celestialQuery = null;
                return;
            }

            fabricService.EnsureEditModeQueries();
            _celestialQuery = fabricService.GetCelestialBodyQuery();
            _cachedWorldSeed = worldSeed;
        }
    }
}
