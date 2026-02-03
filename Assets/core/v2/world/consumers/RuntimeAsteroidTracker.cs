using System;
using UnityEngine;
using Starfire.Core.V2.Save;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Data;
using Starfire.Core.V2.World.Simulation;
using StarfireV2;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Tracks the runtime state of a spawned asteroid.
    /// Monitors chunk boundary crossings and state modifications.
    /// Handles self-unload when asteroid escapes into unloaded chunks.
    /// Extends RuntimeEntityTrackerBase for common tracking functionality.
    /// </summary>
    public class RuntimeAsteroidTracker : RuntimeEntityTrackerBase
    {
        // ── IRuntimeEntityTracker Implementation ────────────────────────────

        public override EntityType EntityType => EntityType.Asteroid;

        public override ISimulatableDefinition Definition => _asteroidDefinition != null
            ? new AsteroidEntityDefinition(_asteroidDefinition.Value)
            : null;

        // ── Asteroid-Specific State ─────────────────────────────────────────

        /// <summary>
        /// The procedural definition of this asteroid (for save/restore matching).
        /// </summary>
        public AsteroidDefinition AsteroidDefinition => _asteroidDefinition ?? default;
        private AsteroidDefinition? _asteroidDefinition;

        /// <summary>
        /// Index of this asteroid within the chunk's asteroid list.
        /// Used for matching against procedural definitions.
        /// </summary>
        public int AsteroidIndex { get; private set; }

        /// <summary>
        /// Legacy event for backward compatibility with existing AsteroidConsumer.
        /// Use the base class OnRequestDestroy via IRuntimeEntityTracker for new code.
        /// </summary>
        public new event Action<RuntimeAsteroidTracker> OnRequestDestroy;

        // ── Initialization ──────────────────────────────────────────────────

        /// <summary>
        /// Initialize the tracker with spawn context.
        /// </summary>
        public void Initialize(ChunkCoord originChunk, AsteroidDefinition definition, int asteroidIndex)
        {
            OriginChunk = originChunk;
            CurrentChunk = originChunk;
            _asteroidDefinition = definition;
            AsteroidIndex = asteroidIndex;
            EntityId = GenerateEntityId(originChunk, asteroidIndex);
            HasBeenModified = false;
            _initialized = true;
        }

        /// <summary>
        /// Initialize the tracker for a promoted (simulated) asteroid.
        /// These are already considered modified since they had velocity.
        /// </summary>
        public void InitializeFromSimulated(ChunkCoord originChunk, ChunkCoord currentChunk,
            AsteroidDefinition definition, int asteroidIndex)
        {
            OriginChunk = originChunk;
            CurrentChunk = currentChunk;
            _asteroidDefinition = definition;
            AsteroidIndex = asteroidIndex;
            EntityId = GenerateEntityId(originChunk, asteroidIndex);
            HasBeenModified = true; // Already modified if it came from simulation
            _initialized = true;
        }

        // ── Override Base Class Methods ─────────────────────────────────────

        public override SimulatedEntity ToSimulatedEntity()
        {
            var entity = CreateBaseSimulatedEntity();

            // Add asteroid-specific data
            if (_asteroidDefinition.HasValue)
            {
                entity.Variant = _asteroidDefinition.Value.Variant;
                entity.Seed = _asteroidDefinition.Value.Seed;
                entity.SourceType = (int)_asteroidDefinition.Value.Source;
                entity.Radius = _asteroidDefinition.Value.Size * 0.5f;
            }

            return entity;
        }

        protected override float GetEntityRadius()
        {
            // Use asteroid size for radius
            if (_asteroidDefinition.HasValue)
            {
                return _asteroidDefinition.Value.Size * 0.5f;
            }
            return base.GetEntityRadius();
        }

        protected override void TransferToSimulation(Vector2D absolutePos)
        {
            // Call base implementation
            base.TransferToSimulation(absolutePos);

            // Also fire legacy event for backward compatibility
            OnRequestDestroy?.Invoke(this);
        }

        protected override void NotifyChunkMigration(ChunkCoord fromChunk, ChunkCoord toChunk)
        {
            // Use asteroid-specific tracking for backward compatibility
            var chunkTracker = SaveSystem.Instance?.ChunkTracker;
            if (chunkTracker == null || !_asteroidDefinition.HasValue) return;

            // Mark as removed from origin chunk
            chunkTracker.OnAsteroidDestroyed(OriginChunk, _asteroidDefinition.Value);

            // Mark as added to destination chunk with current velocity
            var velocity = _rb != null ? _rb.linearVelocity : Vector2.zero;
            var angularVelocity = _rb != null ? _rb.angularVelocity : 0f;
            chunkTracker.OnAsteroidAdded(toChunk, CreateModifiedDefinition(velocity, angularVelocity), velocity, angularVelocity);

            Debug.Log($"[RuntimeAsteroidTracker] Asteroid migrated from chunk ({fromChunk.X}, {fromChunk.Y}) to ({toChunk.X}, {toChunk.Y})");
        }

        protected override void MarkAsModified()
        {
            if (HasBeenModified) return;

            HasBeenModified = true;

            // Notify the chunk tracker about the modification using asteroid-specific method
            var chunkTracker = SaveSystem.Instance?.ChunkTracker;
            if (chunkTracker != null && _rb != null && _asteroidDefinition.HasValue)
            {
                chunkTracker.OnAsteroidModified(OriginChunk, _asteroidDefinition.Value, _rb.linearVelocity, _rb.angularVelocity);
            }

            Debug.Log($"[RuntimeAsteroidTracker] Asteroid marked as modified in chunk ({OriginChunk.X}, {OriginChunk.Y})");
        }

        // ── Asteroid-Specific Helpers ───────────────────────────────────────

        /// <summary>
        /// Create a new AsteroidDefinition with updated local position for the destination chunk.
        /// </summary>
        private AsteroidDefinition CreateModifiedDefinition(Vector2 velocity, float angularVelocity)
        {
            if (!_asteroidDefinition.HasValue)
                return default;

            var service = WorldGenerationService.Instance;
            var chunkSize = service?.ChunkSize ?? 1000f;

            // Calculate local position relative to current chunk center
            var absolutePos = service.WorldToAbsolute(transform.position);
            var chunkCenterAbs = new Vector2D(
                CurrentChunk.X * chunkSize + chunkSize * 0.5,
                CurrentChunk.Y * chunkSize + chunkSize * 0.5
            );
            var localPos = absolutePos - chunkCenterAbs;

            return new AsteroidDefinition
            {
                LocalPosition = localPos.ToVector2(),
                Size = _asteroidDefinition.Value.Size,
                Rotation = _rb != null ? _rb.rotation : _asteroidDefinition.Value.Rotation,
                Variant = _asteroidDefinition.Value.Variant,
                Seed = _asteroidDefinition.Value.Seed,
                Source = _asteroidDefinition.Value.Source
            };
        }

        public override void ResetTracker()
        {
            base.ResetTracker();
            _asteroidDefinition = null;
            AsteroidIndex = 0;
        }
    }
}
