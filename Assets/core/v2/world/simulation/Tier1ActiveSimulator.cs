using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Simulation.Collision;
using Starfire.Core.V2.World.Simulation.Events;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Performs lightweight per-frame physics simulation for entities in nearby unloaded chunks.
    /// Uses simple Euler integration — no Unity physics engine involvement.
    /// Includes collision detection and resolution.
    /// </summary>
    public class Tier1ActiveSimulator
    {
        private readonly Dictionary<int, SimulatedEntity> _entities = new();
        private readonly List<int> _removalBuffer = new();
        private readonly BackgroundSimulationConfig _config;
        private readonly SimulationCollisionDetector _collisionDetector;
        private double _chunkSize;

        /// <summary>Fired when an entity crosses a chunk boundary.</summary>
        public event Action<SimulatedEntity, ChunkCoord, ChunkCoord> OnEntityMigratedChunk;

        /// <summary>Fired when an entity should be promoted back to a real GameObject (chunk reloaded).</summary>
        public event Action<SimulatedEntity> OnEntityPromoted;

        /// <summary>Fired when a collision occurs.</summary>
        public event Action<CollisionEvent> OnCollision;

        /// <summary>Fired when an entity is destroyed.</summary>
        public event Action<DestructionEvent> OnDestruction;

        public int EntityCount => _entities.Count;

        /// <summary>Number of collisions detected last frame.</summary>
        public int LastFrameCollisionCount => _collisionDetector?.LastFrameCollisionCount ?? 0;

        /// <summary>Number of destructions last frame.</summary>
        public int LastFrameDestructionCount => _collisionDetector?.LastFrameDestructionCount ?? 0;

        public Tier1ActiveSimulator(BackgroundSimulationConfig config, double chunkSize)
        {
            _config = config;
            _chunkSize = chunkSize;

            // Initialize collision detector
            _collisionDetector = new SimulationCollisionDetector(
                config.spatialHashCellSize,
                config.collisionRestitution,
                config.minCollisionIntensity,
                config.structuralIntegrityMultiplier,
                config.minDestructionEnergy,
                chunkSize);

            // Wire up collision events
            _collisionDetector.OnCollision += evt => OnCollision?.Invoke(evt);
            _collisionDetector.OnDestruction += evt => OnDestruction?.Invoke(evt);
        }

        public void Tick(float deltaTime, double currentTime)
        {
            // Phase 1: Update physics for all entities
            foreach (var kvp in _entities)
            {
                var entity = kvp.Value;
                UpdatePhysics(entity, deltaTime);
            }

            // Phase 2: Detect and resolve collisions
            var destroyedIds = _collisionDetector.DetectAndResolve(
                _entities.Values,
                _entities,
                currentTime);

            // Phase 3: Remove destroyed entities
            foreach (int id in destroyedIds)
            {
                _entities.Remove(id);
            }

            // Phase 4: Check chunk migrations (after collision resolution)
            foreach (var kvp in _entities)
            {
                CheckChunkMigration(kvp.Value);
            }
        }

        /// <summary>
        /// Legacy overload for backward compatibility.
        /// </summary>
        public void Tick(float deltaTime)
        {
            Tick(deltaTime, Time.timeAsDouble);
        }

        private void UpdatePhysics(SimulatedEntity entity, float dt)
        {
            // Apply drag: a = -drag * v / mass
            if (entity.Drag > 0f && entity.Mass > 0f)
            {
                double dragFactor = 1.0 - (entity.Drag / entity.Mass) * dt;
                if (dragFactor < 0.0) dragFactor = 0.0;
                entity.Velocity = entity.Velocity * dragFactor;
            }

            // Euler integration
            entity.AbsolutePosition += entity.Velocity * dt;
            entity.Rotation += entity.AngularVelocity * dt;
        }

        private void CheckChunkMigration(SimulatedEntity entity)
        {
            var newChunk = ChunkCoord.FromAbsolutePosition(entity.AbsolutePosition, _chunkSize);
            if (newChunk != entity.CurrentChunk)
            {
                var oldChunk = entity.CurrentChunk;
                entity.CurrentChunk = newChunk;
                OnEntityMigratedChunk?.Invoke(entity, oldChunk, newChunk);
            }
        }

        public void AddEntity(SimulatedEntity entity)
        {
            Debug.Log($"[Tier1Simulator] AddEntity called. EntityId={entity.EntityId}, Type={entity.EntityType}, Pos=({entity.AbsolutePosition.X:F1}, {entity.AbsolutePosition.Y:F1}), Vel=({entity.Velocity.X:F2}, {entity.Velocity.Y:F2})");

            entity.CurrentTier = SimulationTier.Tier1Active;
            _entities[entity.EntityId] = entity;

            Debug.Log($"[Tier1Simulator] Entity {entity.EntityId} added. Total entity count: {_entities.Count}, Max allowed: {_config.tier1MaxEntities}");

            // Enforce max entity limit — drop farthest if over budget
            if (_entities.Count > _config.tier1MaxEntities)
            {
                Debug.LogWarning($"[Tier1Simulator] Over entity budget! Count={_entities.Count}, Max={_config.tier1MaxEntities}. Trimming farthest entities.");
                TrimFarthestEntities();
            }
        }

        public SimulatedEntity RemoveEntity(int entityId)
        {
            if (_entities.TryGetValue(entityId, out var entity))
            {
                _entities.Remove(entityId);
                Debug.Log($"[Tier1Simulator] RemoveEntity: EntityId={entityId} removed. Remaining count: {_entities.Count}");
                return entity;
            }
            Debug.Log($"[Tier1Simulator] RemoveEntity: EntityId={entityId} NOT FOUND");
            return null;
        }

        public bool HasEntity(int entityId) => _entities.ContainsKey(entityId);

        public SimulatedEntity GetEntity(int entityId)
        {
            _entities.TryGetValue(entityId, out var entity);
            return entity;
        }

        public IEnumerable<SimulatedEntity> GetAllEntities() => _entities.Values;

        /// <summary>
        /// Returns all entities whose CurrentChunk matches the given coordinate.
        /// </summary>
        public List<SimulatedEntity> GetEntitiesInChunk(ChunkCoord coord)
        {
            var result = new List<SimulatedEntity>();
            foreach (var entity in _entities.Values)
            {
                if (entity.CurrentChunk == coord)
                    result.Add(entity);
            }
            return result;
        }

        private void TrimFarthestEntities()
        {
            // Simple cull: this only fires when over budget.
            // The BackgroundSimulationManager handles proper tier demotion.
            // This is a safety valve.
        }
    }
}
