using System.Collections.Generic;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Simulation.Config;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation.Behaviors
{
    /// <summary>
    /// Provides access to simulation world state for behaviors.
    /// Passed to behavior UpdateEntity and CaptureSnapshot methods.
    /// </summary>
    public interface ISimulationContext
    {
        // ── Time ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Current simulation time (game time, not real time).
        /// </summary>
        double CurrentTime { get; }

        /// <summary>
        /// Delta time for current frame in seconds.
        /// </summary>
        float DeltaTime { get; }

        // ── World ────────────────────────────────────────────────────────────

        /// <summary>
        /// Size of a chunk in world units.
        /// </summary>
        double ChunkSize { get; }

        /// <summary>
        /// Current player chunk coordinate.
        /// </summary>
        ChunkCoord PlayerChunk { get; }

        /// <summary>
        /// Current player absolute position.
        /// </summary>
        Vector2D PlayerPosition { get; }

        // ── Entity Queries ───────────────────────────────────────────────────

        /// <summary>
        /// Try to get an entity by ID from the simulation.
        /// </summary>
        /// <param name="entityId">The entity ID to look up.</param>
        /// <param name="entity">The found entity, or null if not found.</param>
        /// <returns>True if the entity was found.</returns>
        bool TryGetEntity(int entityId, out SimulatedEntity entity);

        /// <summary>
        /// Get all entities within a radius of a position.
        /// </summary>
        /// <param name="center">Center position for the search.</param>
        /// <param name="radius">Search radius in world units.</param>
        /// <returns>Enumerable of entities within the radius.</returns>
        IEnumerable<SimulatedEntity> GetEntitiesInRadius(Vector2D center, float radius);

        /// <summary>
        /// Get all entities of a specific type.
        /// </summary>
        /// <param name="type">The entity type to filter by.</param>
        /// <returns>Enumerable of entities of the specified type.</returns>
        IEnumerable<SimulatedEntity> GetEntitiesOfType(EntityType type);

        /// <summary>
        /// Get all entities in a specific chunk.
        /// </summary>
        /// <param name="chunk">The chunk coordinate.</param>
        /// <returns>Enumerable of entities in the chunk.</returns>
        IEnumerable<SimulatedEntity> GetEntitiesInChunk(ChunkCoord chunk);

        /// <summary>
        /// Get all simulated entities.
        /// </summary>
        IEnumerable<SimulatedEntity> GetAllEntities();

        // ── Config Access ────────────────────────────────────────────────────

        /// <summary>
        /// Get the entity type configuration for a specific type.
        /// </summary>
        /// <param name="type">The entity type.</param>
        /// <returns>The configuration, or null if not registered.</returns>
        SimulationEntityTypeConfig GetEntityConfig(EntityType type);

        /// <summary>
        /// Get a behavior instance by type.
        /// </summary>
        /// <param name="behaviorType">The behavior type.</param>
        /// <returns>The behavior instance, or null if not registered.</returns>
        ISimulationBehavior GetBehavior(SimulatedBehaviorType behaviorType);

        // ── Combat ───────────────────────────────────────────────────────────

        /// <summary>
        /// Apply damage to an entity.
        /// </summary>
        /// <param name="targetId">ID of the entity to damage.</param>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <param name="instigatorId">ID of the entity causing the damage (optional).</param>
        void ApplyDamage(int targetId, float damage, int? instigatorId);

        /// <summary>
        /// Record a combat event for later processing/reporting.
        /// </summary>
        /// <param name="eventData">The combat event data.</param>
        void RecordCombatEvent(CombatEventData eventData);

        // ── Entity Management ────────────────────────────────────────────────

        /// <summary>
        /// Register a new entity with the simulation.
        /// </summary>
        /// <param name="entity">The entity to register.</param>
        void RegisterEntity(SimulatedEntity entity);

        /// <summary>
        /// Unregister an entity from the simulation.
        /// </summary>
        /// <param name="entityId">ID of the entity to unregister.</param>
        void UnregisterEntity(int entityId);

        /// <summary>
        /// Mark an entity for destruction at the end of the current tick.
        /// </summary>
        /// <param name="entityId">ID of the entity to destroy.</param>
        /// <param name="instigatorId">ID of the entity that caused the destruction (optional).</param>
        void MarkForDestruction(int entityId, int? instigatorId);
    }

    /// <summary>
    /// Data for a combat event that occurred in simulation.
    /// </summary>
    public struct CombatEventData
    {
        /// <summary>ID of the attacking entity.</summary>
        public int AttackerId;

        /// <summary>ID of the target entity.</summary>
        public int TargetId;

        /// <summary>Amount of damage dealt.</summary>
        public float Damage;

        /// <summary>Time the event occurred.</summary>
        public double Time;

        /// <summary>Position where the event occurred.</summary>
        public Vector2D Position;

        /// <summary>Type of damage (for categorization).</summary>
        public CombatDamageType DamageType;

        /// <summary>Whether this attack resulted in target destruction.</summary>
        public bool WasKillingBlow;
    }

    /// <summary>
    /// Types of combat damage for categorization.
    /// </summary>
    public enum CombatDamageType
    {
        /// <summary>Standard weapon damage.</summary>
        Weapon,
        /// <summary>Collision damage.</summary>
        Collision,
        /// <summary>Explosion/area damage.</summary>
        Explosion,
        /// <summary>Environmental damage.</summary>
        Environmental
    }
}
