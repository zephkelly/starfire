using System.Collections.Generic;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;

namespace Starfire.Core.V2.Save.Tracking
{
    public class ChunkModificationTracker
    {
        private readonly Dictionary<ChunkCoord, ChunkModificationData> _modifications = new();

        public void OnAsteroidDestroyed(ChunkCoord chunk, AsteroidDefinition asteroid)
        {
            var mod = GetOrCreate(chunk);
            mod.ModificationFlags |= ChunkModificationFlags.AsteroidsRemoved;
            mod.AsteroidModifications ??= new List<AsteroidModification>();
            mod.AsteroidModifications.Add(new AsteroidModification
            {
                Type = AsteroidModificationType.Removed,
                LocalPositionX = asteroid.LocalPosition.x,
                LocalPositionY = asteroid.LocalPosition.y,
                Size = asteroid.Size,
                Rotation = asteroid.Rotation,
                Variant = asteroid.Variant,
                Seed = asteroid.Seed,
                SourceType = (int)asteroid.Source
            });
        }

        public void OnAsteroidAdded(ChunkCoord chunk, AsteroidDefinition asteroid)
        {
            var mod = GetOrCreate(chunk);
            mod.ModificationFlags |= ChunkModificationFlags.AsteroidsAdded;
            mod.AsteroidModifications ??= new List<AsteroidModification>();
            mod.AsteroidModifications.Add(new AsteroidModification
            {
                Type = AsteroidModificationType.Added,
                LocalPositionX = asteroid.LocalPosition.x,
                LocalPositionY = asteroid.LocalPosition.y,
                Size = asteroid.Size,
                Rotation = asteroid.Rotation,
                Variant = asteroid.Variant,
                Seed = asteroid.Seed,
                SourceType = (int)asteroid.Source
            });
        }

        public bool IsChunkModified(ChunkCoord coord) => _modifications.ContainsKey(coord);

        public IEnumerable<ChunkModificationData> GetAllModifications() => _modifications.Values;

        public ChunkModificationData GetModification(ChunkCoord coord)
        {
            _modifications.TryGetValue(coord, out var mod);
            return mod;
        }

        public void RestoreModification(ChunkModificationData mod)
        {
            var coord = mod.ChunkCoord;
            _modifications[coord] = mod;
        }

        public void Clear() => _modifications.Clear();

        private ChunkModificationData GetOrCreate(ChunkCoord coord)
        {
            if (!_modifications.TryGetValue(coord, out var mod))
            {
                mod = new ChunkModificationData { ChunkCoord = coord };
                _modifications[coord] = mod;
            }
            return mod;
        }
    }
}
