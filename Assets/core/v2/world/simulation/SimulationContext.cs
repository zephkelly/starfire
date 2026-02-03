using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Simulation.Behaviors;
using Starfire.Core.V2.World.Simulation.Config;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Concrete implementation of ISimulationContext.
    /// Provides access to simulation world state for behaviors during Tier 1 updates.
    /// </summary>
    public class SimulationContext : ISimulationContext
    {
        private readonly SimulationEntityTypeRegistry _registry;
        private readonly Func<int, SimulatedEntity> _entityLookup;
        private readonly Func<IEnumerable<SimulatedEntity>> _getAllEntities;
        private readonly Action<SimulatedEntity> _registerEntity;
        private readonly Action<int> _unregisterEntity;
        private readonly Action<int, int?> _markForDestruction;

        private readonly List<CombatEventData> _combatEvents = new();
        private readonly HashSet<int> _entitiesMarkedForDestruction = new();
        private readonly Dictionary<int, (float damage, int? instigator)> _pendingDamage = new();

        public double CurrentTime { get; set; }
        public float DeltaTime { get; set; }
        public double ChunkSize { get; set; }
        public ChunkCoord PlayerChunk { get; set; }
        public Vector2D PlayerPosition { get; set; }

        /// <summary>
        /// Combat events recorded this frame.
        /// </summary>
        public IReadOnlyList<CombatEventData> CombatEvents => _combatEvents;

        /// <summary>
        /// Entity IDs marked for destruction this frame.
        /// </summary>
        public IReadOnlyCollection<int> EntitiesMarkedForDestruction => _entitiesMarkedForDestruction;

        /// <summary>
        /// Pending damage to apply to entities.
        /// </summary>
        public IReadOnlyDictionary<int, (float damage, int? instigator)> PendingDamage => _pendingDamage;

        public SimulationContext(
            SimulationEntityTypeRegistry registry,
            Func<int, SimulatedEntity> entityLookup,
            Func<IEnumerable<SimulatedEntity>> getAllEntities,
            Action<SimulatedEntity> registerEntity,
            Action<int> unregisterEntity,
            Action<int, int?> markForDestruction)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _entityLookup = entityLookup ?? throw new ArgumentNullException(nameof(entityLookup));
            _getAllEntities = getAllEntities ?? throw new ArgumentNullException(nameof(getAllEntities));
            _registerEntity = registerEntity ?? throw new ArgumentNullException(nameof(registerEntity));
            _unregisterEntity = unregisterEntity ?? throw new ArgumentNullException(nameof(unregisterEntity));
            _markForDestruction = markForDestruction ?? throw new ArgumentNullException(nameof(markForDestruction));
        }

        /// <summary>
        /// Clear frame-specific data at the start of a new tick.
        /// </summary>
        public void BeginFrame(double currentTime, float deltaTime)
        {
            CurrentTime = currentTime;
            DeltaTime = deltaTime;
            _combatEvents.Clear();
            _entitiesMarkedForDestruction.Clear();
            _pendingDamage.Clear();
        }

        /// <summary>
        /// Update player state.
        /// </summary>
        public void UpdatePlayerState(ChunkCoord playerChunk, Vector2D playerPosition)
        {
            PlayerChunk = playerChunk;
            PlayerPosition = playerPosition;
        }

        // ── Entity Queries ───────────────────────────────────────────────────

        public bool TryGetEntity(int entityId, out SimulatedEntity entity)
        {
            entity = _entityLookup(entityId);
            return entity != null;
        }

        public IEnumerable<SimulatedEntity> GetEntitiesInRadius(Vector2D center, float radius)
        {
            double radiusSqr = radius * radius;
            foreach (var entity in _getAllEntities())
            {
                var delta = entity.AbsolutePosition - center;
                if (delta.SqrMagnitude <= radiusSqr)
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<SimulatedEntity> GetEntitiesOfType(EntityType type)
        {
            foreach (var entity in _getAllEntities())
            {
                if (entity.EntityType == type)
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<SimulatedEntity> GetEntitiesInChunk(ChunkCoord chunk)
        {
            foreach (var entity in _getAllEntities())
            {
                if (entity.CurrentChunk == chunk)
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<SimulatedEntity> GetAllEntities()
        {
            return _getAllEntities();
        }

        // ── Config Access ────────────────────────────────────────────────────

        public SimulationEntityTypeConfig GetEntityConfig(EntityType type)
        {
            return _registry.GetConfig(type);
        }

        public ISimulationBehavior GetBehavior(SimulatedBehaviorType behaviorType)
        {
            return _registry.GetBehavior(behaviorType);
        }

        // ── Combat ───────────────────────────────────────────────────────────

        public void ApplyDamage(int targetId, float damage, int? instigatorId)
        {
            if (!_pendingDamage.TryGetValue(targetId, out var existing))
            {
                _pendingDamage[targetId] = (damage, instigatorId);
            }
            else
            {
                // Accumulate damage, keep first instigator
                _pendingDamage[targetId] = (existing.damage + damage, existing.instigator ?? instigatorId);
            }
        }

        public void RecordCombatEvent(CombatEventData eventData)
        {
            eventData.Time = CurrentTime;
            _combatEvents.Add(eventData);
        }

        // ── Entity Management ────────────────────────────────────────────────

        public void RegisterEntity(SimulatedEntity entity)
        {
            _registerEntity(entity);
        }

        public void UnregisterEntity(int entityId)
        {
            _unregisterEntity(entityId);
        }

        public void MarkForDestruction(int entityId, int? instigatorId)
        {
            _entitiesMarkedForDestruction.Add(entityId);
            _markForDestruction(entityId, instigatorId);
        }

        // ── Utility ──────────────────────────────────────────────────────────

        /// <summary>
        /// Get the distance (in chunks) from player to a position.
        /// </summary>
        public int GetChunkDistanceFromPlayer(ChunkCoord chunk)
        {
            return (int)PlayerChunk.ChebyshevDistance(chunk);
        }

        /// <summary>
        /// Get the distance (in chunks) from player to an entity.
        /// </summary>
        public int GetChunkDistanceFromPlayer(SimulatedEntity entity)
        {
            return (int)PlayerChunk.ChebyshevDistance(entity.CurrentChunk);
        }
    }
}
