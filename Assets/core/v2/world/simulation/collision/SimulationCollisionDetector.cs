using System;
using System.Collections.Generic;
using Starfire.Core.V2.World.Simulation.Events;
using UnityEngine;

namespace Starfire.Core.V2.World.Simulation.Collision
{
    /// <summary>
    /// Detects and resolves collisions between simulated entities.
    /// Uses spatial hashing for broad phase and circle-circle for narrow phase.
    /// </summary>
    public class SimulationCollisionDetector
    {
        private readonly SpatialHashGrid _grid;
        private readonly float _restitution;
        private readonly float _minIntensity;
        private readonly float _structuralIntegrityMultiplier;
        private readonly float _minDestructionEnergy;
        private readonly double _chunkSize;

        private readonly HashSet<(int, int)> _processedPairs = new();
        private readonly List<int> _entitiesToDestroy = new();

        /// <summary>Fired when a collision occurs.</summary>
        public event Action<CollisionEvent> OnCollision;

        /// <summary>Fired when an entity is destroyed.</summary>
        public event Action<DestructionEvent> OnDestruction;

        /// <summary>Number of collisions detected this frame.</summary>
        public int LastFrameCollisionCount { get; private set; }

        /// <summary>Number of destructions this frame.</summary>
        public int LastFrameDestructionCount { get; private set; }

        public SimulationCollisionDetector(
            double cellSize,
            float restitution,
            float minIntensity,
            float structuralIntegrityMultiplier,
            float minDestructionEnergy,
            double chunkSize)
        {
            _grid = new SpatialHashGrid(cellSize);
            _restitution = restitution;
            _minIntensity = minIntensity;
            _structuralIntegrityMultiplier = structuralIntegrityMultiplier;
            _minDestructionEnergy = minDestructionEnergy;
            _chunkSize = chunkSize;
        }

        /// <summary>
        /// Detects and resolves collisions for all entities.
        /// Modifies entity velocities and positions in place.
        /// Returns list of entity IDs that should be removed (destroyed).
        /// </summary>
        public List<int> DetectAndResolve(
            IEnumerable<SimulatedEntity> entities,
            Dictionary<int, SimulatedEntity> entityLookup,
            double currentTime)
        {
            _processedPairs.Clear();
            _entitiesToDestroy.Clear();
            LastFrameCollisionCount = 0;
            LastFrameDestructionCount = 0;

            // Broad phase: populate spatial grid
            _grid.Clear();
            foreach (var entity in entities)
            {
                _grid.Insert(entity.EntityId, entity.AbsolutePosition);
            }

            // Narrow phase: check each entity against nearby entities
            foreach (var entity in entities)
            {
                if (_entitiesToDestroy.Contains(entity.EntityId))
                    continue;

                var nearby = _grid.GetPotentialColliders(entity.AbsolutePosition, entity.Radius);

                foreach (int otherId in nearby)
                {
                    // Skip self
                    if (otherId == entity.EntityId)
                        continue;

                    // Skip already destroyed
                    if (_entitiesToDestroy.Contains(otherId))
                        continue;

                    // Skip already processed pairs
                    var pair = entity.EntityId < otherId
                        ? (entity.EntityId, otherId)
                        : (otherId, entity.EntityId);

                    if (_processedPairs.Contains(pair))
                        continue;

                    _processedPairs.Add(pair);

                    // Get the other entity
                    if (!entityLookup.TryGetValue(otherId, out var other))
                        continue;

                    // Narrow phase: circle-circle collision check
                    ProcessCollision(entity, other, currentTime);
                }
            }

            return _entitiesToDestroy;
        }

        private void ProcessCollision(SimulatedEntity entityA, SimulatedEntity entityB, double currentTime)
        {
            if (!CollisionMath.CheckCircleCollision(
                entityA.AbsolutePosition, entityA.Radius,
                entityB.AbsolutePosition, entityB.Radius,
                out double penetration,
                out Vector2D normal))
            {
                return;
            }

            // Store pre-collision velocities
            Vector2D preVelA = entityA.Velocity;
            Vector2D preVelB = entityB.Velocity;

            // Calculate collision metrics
            float intensity = (float)CollisionMath.CalculateCollisionIntensity(preVelA, preVelB);
            float impactEnergy = (float)CollisionMath.CalculateImpactEnergy(
                preVelA, entityA.Mass,
                preVelB, entityB.Mass);

            // Skip very low-intensity collisions
            if (intensity < _minIntensity)
                return;

            // Resolve collision (update velocities)
            Vector2D velA = preVelA;
            Vector2D velB = preVelB;
            CollisionMath.ResolveElasticCollision(
                ref velA, entityA.Mass,
                ref velB, entityB.Mass,
                normal, _restitution);

            // Separate overlapping entities
            Vector2D posA = entityA.AbsolutePosition;
            Vector2D posB = entityB.AbsolutePosition;
            CollisionMath.SeparateOverlap(
                ref posA, entityA.Mass,
                ref posB, entityB.Mass,
                normal, penetration);

            // Check for destruction
            float integrityA = entityA.Mass * _structuralIntegrityMultiplier;
            float integrityB = entityB.Mass * _structuralIntegrityMultiplier;

            bool destroyA = impactEnergy >= _minDestructionEnergy &&
                            CollisionMath.ShouldDestroy(impactEnergy, integrityA);
            bool destroyB = impactEnergy >= _minDestructionEnergy &&
                            CollisionMath.ShouldDestroy(impactEnergy, integrityB);

            // Determine collision result
            CollisionResult result;
            if (destroyA && destroyB)
                result = CollisionResult.BothDestroyed;
            else if (destroyA)
                result = CollisionResult.EntityADestroyed;
            else if (destroyB)
                result = CollisionResult.EntityBDestroyed;
            else
                result = CollisionResult.Bounced;

            // Determine instigator (prefer the one that was already moving with an instigator)
            int? instigatorId = entityA.InstigatorEntityId ?? entityB.InstigatorEntityId;

            // Apply changes to surviving entities
            if (!destroyA)
            {
                entityA.Velocity = velA;
                entityA.AbsolutePosition = posA;

                // Transfer instigator if entity B had one
                if (entityB.InstigatorEntityId.HasValue && !entityA.InstigatorEntityId.HasValue)
                {
                    entityA.InstigatorEntityId = entityB.InstigatorEntityId;
                }
            }
            else
            {
                _entitiesToDestroy.Add(entityA.EntityId);
                LastFrameDestructionCount++;

                // Fire destruction event
                var destructionEvt = DestructionEvent.Create(
                    entityA, entityB, impactEnergy, instigatorId, currentTime, _chunkSize);
                OnDestruction?.Invoke(destructionEvt);
            }

            if (!destroyB)
            {
                entityB.Velocity = velB;
                entityB.AbsolutePosition = posB;

                // Transfer instigator if entity A had one
                if (entityA.InstigatorEntityId.HasValue && !entityB.InstigatorEntityId.HasValue)
                {
                    entityB.InstigatorEntityId = entityA.InstigatorEntityId;
                }
            }
            else
            {
                _entitiesToDestroy.Add(entityB.EntityId);
                LastFrameDestructionCount++;

                // Fire destruction event
                var destructionEvt = DestructionEvent.Create(
                    entityB, entityA, impactEnergy, instigatorId, currentTime, _chunkSize);
                OnDestruction?.Invoke(destructionEvt);
            }

            // Record collision event
            var collisionEvt = CollisionEvent.Create(
                entityA, entityB,
                destroyA ? Vector2D.Zero : velA,
                destroyB ? Vector2D.Zero : velB,
                intensity, impactEnergy,
                result, currentTime, _chunkSize);
            OnCollision?.Invoke(collisionEvt);
            LastFrameCollisionCount++;
        }
    }
}
