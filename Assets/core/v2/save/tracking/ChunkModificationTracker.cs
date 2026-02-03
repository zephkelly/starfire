using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.V2.World.Simulation;
using StarfireV2;

namespace Starfire.Core.V2.Save.Tracking
{
    public class ChunkModificationTracker
    {
        private readonly Dictionary<ChunkCoord, ChunkModificationData> _modifications = new();

        // Hash-based index for O(1) modification lookup: (chunkHash, positionHash) → list index
        private readonly Dictionary<(long chunkHash, long positionHash), int> _modificationIndex = new();

        // Quantization scale for position hashing (100 = 0.01 unit precision)
        private const float PositionQuantizationScale = 100f;

        // ── Asteroid-Specific Methods (Backward Compatibility) ──────────────

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
            OnAsteroidAdded(chunk, asteroid, Vector2.zero, 0f);
        }

        /// <summary>
        /// Called when an asteroid is added to a chunk (migrated here from elsewhere).
        /// </summary>
        public void OnAsteroidAdded(ChunkCoord chunk, AsteroidDefinition asteroid, Vector2 velocity, float angularVelocity)
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
                SourceType = (int)asteroid.Source,
                VelocityX = velocity.x,
                VelocityY = velocity.y,
                AngularVelocity = angularVelocity
            });
        }

        /// <summary>
        /// Called when an asteroid's state has been modified (e.g., velocity changed due to collision).
        /// Uses hash-based lookup for O(1) duplicate detection.
        /// </summary>
        public void OnAsteroidModified(ChunkCoord chunk, AsteroidDefinition asteroid, Vector2 velocity, float angularVelocity)
        {
            var mod = GetOrCreate(chunk);
            mod.ModificationFlags |= ChunkModificationFlags.AsteroidsModified;
            mod.AsteroidModifications ??= new List<AsteroidModification>();

            // Use hash-based lookup for O(1) duplicate detection
            long chunkHash = GetChunkHash(chunk);
            long positionHash = HashPosition(asteroid.LocalPosition.x, asteroid.LocalPosition.y);
            var indexKey = (chunkHash, positionHash);

            if (_modificationIndex.TryGetValue(indexKey, out int existingIndex) &&
                existingIndex < mod.AsteroidModifications.Count)
            {
                var existing = mod.AsteroidModifications[existingIndex];
                if (existing.Type == AsteroidModificationType.Modified)
                {
                    // Update existing modification with latest velocity
                    existing.VelocityX = velocity.x;
                    existing.VelocityY = velocity.y;
                    existing.AngularVelocity = angularVelocity;
                    mod.AsteroidModifications[existingIndex] = existing;
                    return;
                }
            }

            // Add new modification and index it
            int newIndex = mod.AsteroidModifications.Count;
            mod.AsteroidModifications.Add(new AsteroidModification
            {
                Type = AsteroidModificationType.Modified,
                LocalPositionX = asteroid.LocalPosition.x,
                LocalPositionY = asteroid.LocalPosition.y,
                Size = asteroid.Size,
                Rotation = asteroid.Rotation,
                Variant = asteroid.Variant,
                Seed = asteroid.Seed,
                SourceType = (int)asteroid.Source,
                VelocityX = velocity.x,
                VelocityY = velocity.y,
                AngularVelocity = angularVelocity
            });
            _modificationIndex[indexKey] = newIndex;
        }

        // ── Generic Entity Methods ──────────────────────────────────────────

        /// <summary>
        /// Called when any entity is destroyed/removed from a chunk.
        /// Generic version that routes to type-specific methods.
        /// </summary>
        public void OnEntityDestroyed(ChunkCoord chunk, ISimulatableDefinition definition)
        {
            if (definition == null) return;

            // Route to type-specific methods for backward compatibility
            if (definition.EntityType == EntityType.Asteroid && definition is AsteroidEntityDefinition asteroidDef)
            {
                OnAsteroidDestroyed(chunk, asteroidDef.ToAsteroidDefinition());
            }
            // Future: Add routing for other entity types
        }

        /// <summary>
        /// Called when any entity is added to a chunk.
        /// Generic version that routes to type-specific methods.
        /// </summary>
        public void OnEntityAdded(ChunkCoord chunk, ISimulatableDefinition definition, Vector2 velocity, float angularVelocity)
        {
            if (definition == null) return;

            if (definition.EntityType == EntityType.Asteroid && definition is AsteroidEntityDefinition asteroidDef)
            {
                OnAsteroidAdded(chunk, asteroidDef.ToAsteroidDefinition(), velocity, angularVelocity);
            }
            // Future: Add routing for other entity types
        }

        /// <summary>
        /// Called when any entity's state is modified.
        /// Generic version that routes to type-specific methods.
        /// </summary>
        public void OnEntityModified(ChunkCoord chunk, ISimulatableDefinition definition, Vector2 velocity, float angularVelocity)
        {
            if (definition == null) return;

            if (definition.EntityType == EntityType.Asteroid && definition is AsteroidEntityDefinition asteroidDef)
            {
                OnAsteroidModified(chunk, asteroidDef.ToAsteroidDefinition(), velocity, angularVelocity);
            }
            // Future: Add routing for other entity types
        }

        /// <summary>
        /// Check if an entity has been removed from a chunk.
        /// Uses hash-based lookup for efficient matching.
        /// </summary>
        public bool IsEntityRemoved(ChunkCoord chunk, ISimulatableDefinition definition)
        {
            if (!_modifications.TryGetValue(chunk, out var mod) || mod.AsteroidModifications == null)
                return false;

            long positionHash = definition.GetPositionHash(PositionQuantizationScale);

            foreach (var asteroidMod in mod.AsteroidModifications)
            {
                if (asteroidMod.Type == AsteroidModificationType.Removed &&
                    HashPosition(asteroidMod.LocalPositionX, asteroidMod.LocalPositionY) == positionHash)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the modification for an entity if it exists.
        /// Uses hash-based lookup for efficient matching.
        /// </summary>
        public AsteroidModification? GetEntityModification(ChunkCoord chunk, ISimulatableDefinition definition)
        {
            if (!_modifications.TryGetValue(chunk, out var mod) || mod.AsteroidModifications == null)
                return null;

            long chunkHash = GetChunkHash(chunk);
            long positionHash = definition.GetPositionHash(PositionQuantizationScale);
            var indexKey = (chunkHash, positionHash);

            if (_modificationIndex.TryGetValue(indexKey, out int index) &&
                index < mod.AsteroidModifications.Count)
            {
                return mod.AsteroidModifications[index];
            }

            // Fallback to linear search if not indexed (for legacy data)
            foreach (var asteroidMod in mod.AsteroidModifications)
            {
                if (asteroidMod.Type == AsteroidModificationType.Modified &&
                    HashPosition(asteroidMod.LocalPositionX, asteroidMod.LocalPositionY) == positionHash)
                {
                    return asteroidMod;
                }
            }

            return null;
        }

        public void OnEntityMigrated(int entityId, ChunkCoord from, ChunkCoord to)
        {
            var mod = GetOrCreate(from);
            mod.ModificationFlags |= ChunkModificationFlags.EntitiesMigrated;
            mod.EntityMigrations ??= new List<EntityMigration>();
            mod.EntityMigrations.Add(new EntityMigration
            {
                EntityId = entityId,
                SourceChunkX = from.X,
                SourceChunkY = from.Y,
                DestinationChunkX = to.X,
                DestinationChunkY = to.Y
            });
        }

        // ── Query Methods ───────────────────────────────────────────────────

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

            // Rebuild index for restored modifications
            RebuildIndexForChunk(coord, mod);
        }

        public void Clear()
        {
            _modifications.Clear();
            _modificationIndex.Clear();
        }

        // ── Private Helpers ─────────────────────────────────────────────────

        private ChunkModificationData GetOrCreate(ChunkCoord coord)
        {
            if (!_modifications.TryGetValue(coord, out var mod))
            {
                mod = new ChunkModificationData { ChunkCoord = coord };
                _modifications[coord] = mod;
            }
            return mod;
        }

        /// <summary>
        /// Generate a stable hash for a chunk coordinate.
        /// </summary>
        private static long GetChunkHash(ChunkCoord coord)
        {
            unchecked
            {
                return (coord.X * 397) ^ coord.Y;
            }
        }

        /// <summary>
        /// Generate a stable hash for a position.
        /// Position is quantized to ensure consistent matching.
        /// </summary>
        private static long HashPosition(float x, float y)
        {
            int qx = Mathf.RoundToInt(x * PositionQuantizationScale);
            int qy = Mathf.RoundToInt(y * PositionQuantizationScale);
            return ((long)qx << 32) | (uint)qy;
        }

        /// <summary>
        /// Rebuild the modification index for a restored chunk.
        /// Called when loading saved data.
        /// </summary>
        private void RebuildIndexForChunk(ChunkCoord coord, ChunkModificationData mod)
        {
            if (mod.AsteroidModifications == null) return;

            long chunkHash = GetChunkHash(coord);

            for (int i = 0; i < mod.AsteroidModifications.Count; i++)
            {
                var asteroidMod = mod.AsteroidModifications[i];
                if (asteroidMod.Type == AsteroidModificationType.Modified)
                {
                    long positionHash = HashPosition(asteroidMod.LocalPositionX, asteroidMod.LocalPositionY);
                    _modificationIndex[(chunkHash, positionHash)] = i;
                }
            }
        }
    }
}
