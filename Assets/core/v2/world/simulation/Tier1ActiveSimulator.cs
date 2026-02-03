using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Simulation.Behaviors;
using Starfire.Core.V2.World.Simulation.Collision;
using Starfire.Core.V2.World.Simulation.Config;
using Starfire.Core.V2.World.Simulation.Events;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Performs lightweight per-frame physics simulation for entities in nearby unloaded chunks.
    /// Uses behavior strategies for entity-type-specific simulation.
    /// Includes collision detection, combat resolution, and shield regeneration.
    /// </summary>
    public class Tier1ActiveSimulator
    {
        private readonly Dictionary<int, SimulatedEntity> _entities = new();
        private readonly List<int> _removalBuffer = new();
        private readonly List<int> _destructionBuffer = new();
        private readonly BackgroundSimulationConfig _config;
        private readonly SimulationCollisionDetector _collisionDetector;
        private readonly double _chunkSize;

        // Data-driven simulation support
        private SimulationEntityTypeRegistry _registry;
        private SimulationContext _context;
        private bool _useBehaviorSystem = false;

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

        /// <summary>
        /// Initialize the behavior-based simulation system.
        /// Call this after construction to enable data-driven entity simulation.
        /// </summary>
        /// <param name="registry">The entity type registry with all configs loaded.</param>
        public void InitializeBehaviorSystem(SimulationEntityTypeRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));

            // Create the simulation context
            _context = new SimulationContext(
                registry,
                entityId => GetEntity(entityId),
                () => GetAllEntities(),
                entity => AddEntity(entity),
                entityId => RemoveEntity(entityId),
                (entityId, instigatorId) => _destructionBuffer.Add(entityId)
            );

            _context.ChunkSize = _chunkSize;
            _useBehaviorSystem = true;

            Debug.Log("[Tier1Simulator] Behavior system initialized.");
        }

        /// <summary>
        /// Whether the behavior-based simulation system is active.
        /// </summary>
        public bool UseBehaviorSystem => _useBehaviorSystem;

        /// <summary>
        /// The simulation context (available when behavior system is initialized).
        /// </summary>
        public SimulationContext Context => _context;

        public void Tick(float deltaTime, double currentTime)
        {
            _destructionBuffer.Clear();

            // Update context if using behavior system
            if (_useBehaviorSystem && _context != null)
            {
                _context.BeginFrame(currentTime, deltaTime);
            }

            // Phase 1: Update physics/behaviors for all entities
            foreach (var kvp in _entities)
            {
                var entity = kvp.Value;

                if (_useBehaviorSystem)
                {
                    UpdateEntityWithBehavior(entity, deltaTime);
                }
                else
                {
                    UpdatePhysicsLegacy(entity, deltaTime);
                }
            }

            // Phase 2: Process pending damage (from combat)
            if (_useBehaviorSystem && _context != null)
            {
                ProcessPendingDamage();
            }

            // Phase 3: Detect and resolve collisions (filter by config if using behavior system)
            IEnumerable<SimulatedEntity> collidableEntities = _entities.Values;
            if (_useBehaviorSystem && _registry != null)
            {
                collidableEntities = _entities.Values.Where(e =>
                {
                    var config = _registry.GetConfig(e.EntityType);
                    return config == null || config.CanCollide;
                });
            }

            var destroyedIds = _collisionDetector.DetectAndResolve(
                collidableEntities,
                _entities,
                currentTime);

            // Phase 4: Remove destroyed entities (from collision + combat + behavior)
            foreach (int id in destroyedIds)
            {
                _entities.Remove(id);
            }

            foreach (int id in _destructionBuffer)
            {
                _entities.Remove(id);
            }

            // Phase 5: Check chunk migrations (after collision resolution)
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

        /// <summary>
        /// Update an entity using the behavior system.
        /// Gets the appropriate behavior from config and delegates to it.
        /// </summary>
        private void UpdateEntityWithBehavior(SimulatedEntity entity, float deltaTime)
        {
            var config = _registry?.GetConfig(entity.EntityType);

            // Get behavior for this entity
            var behaviorSnapshot = entity.GetBehaviorSnapshot();
            var behaviorType = behaviorSnapshot?.Type ?? SimulatedBehaviorType.Ballistic;
            var behavior = _registry?.GetBehavior(behaviorType);

            if (behavior != null)
            {
                // Use behavior-based update
                behavior.UpdateEntity(entity, deltaTime, _context);
            }
            else
            {
                // Fallback to legacy physics
                UpdatePhysicsLegacy(entity, deltaTime);
            }

            // Handle shields if configured
            if (config != null && config.HasShields && config.RegenerateShieldsInSimulation)
            {
                UpdateShieldRegeneration(entity, config, deltaTime);
            }

            // Handle lifetime if configured
            if (config != null && config.HasLifetime)
            {
                UpdateLifetime(entity, config, deltaTime);
            }
        }

        /// <summary>
        /// Update shield regeneration for an entity.
        /// Uses ship capabilities when available for ships, falls back to config defaults.
        /// </summary>
        private void UpdateShieldRegeneration(SimulatedEntity entity, SimulationEntityTypeConfig config, float deltaTime)
        {
            // For ships with extracted capabilities, use module-derived values
            if (entity.EntityType == EntityType.Ship && entity.HasShipCapabilities())
            {
                var caps = entity.GetShipCapabilities();

                // Skip if no shields
                if (!caps.HasShields) return;

                // Check regen delay
                double lastDamageTime = entity.GetTypeData("LastShieldDamageTime", 0.0);
                if (_context != null && _context.CurrentTime - lastDamageTime < caps.ShieldRechargeDelay)
                {
                    return; // Still in damage cooldown
                }

                // Regenerate using module values
                if (caps.CurrentShield < caps.MaxShield)
                {
                    caps.CurrentShield = Mathf.Min(caps.CurrentShield + caps.ShieldRegenRate * deltaTime, caps.MaxShield);
                    entity.SetShipCapabilities(caps);
                }
                return;
            }

            // Fallback: Use config-based values for non-ships or ships without capabilities
            float currentShields = entity.GetTypeData("CurrentShields", 0f);
            float maxShields = entity.GetTypeData("MaxShields", config.DefaultMaxShields);
            double configLastDamageTime = entity.GetTypeData("LastShieldDamageTime", 0.0);

            // Check if we're past the regen delay
            if (_context != null && _context.CurrentTime - configLastDamageTime < config.ShieldRegenDelay)
            {
                return; // Still in damage cooldown
            }

            // Regenerate shields
            if (currentShields < maxShields)
            {
                currentShields = Mathf.Min(currentShields + config.ShieldRegenRate * deltaTime, maxShields);
                entity.SetTypeData("CurrentShields", currentShields);
            }
        }

        /// <summary>
        /// Update lifetime for an entity (e.g., projectiles).
        /// </summary>
        private void UpdateLifetime(SimulatedEntity entity, SimulationEntityTypeConfig config, float deltaTime)
        {
            float lifetime = entity.GetLifetimeRemaining();

            // If lifetime not set, initialize it
            if (lifetime <= 0f && !entity.HasTypeData("LifetimeInitialized"))
            {
                lifetime = config.DefaultLifetime;
                entity.SetLifetimeRemaining(lifetime);
                entity.SetTypeData("LifetimeInitialized", true);
            }

            // Decrement lifetime
            lifetime -= deltaTime;
            entity.SetLifetimeRemaining(lifetime);

            // Check expiration
            if (lifetime <= 0f)
            {
                switch (config.OnLifetimeExpired)
                {
                    case LifetimeExpiredAction.Destroy:
                        _destructionBuffer.Add(entity.EntityId);
                        break;

                    case LifetimeExpiredAction.Stop:
                        entity.Velocity = Vector2D.Zero;
                        entity.AngularVelocity = 0f;
                        break;

                    case LifetimeExpiredAction.MarkExpired:
                        entity.SetTypeData("Expired", true);
                        break;
                }
            }
        }

        /// <summary>
        /// Process pending damage from combat events.
        /// Uses ship capabilities when available for ships, falls back to config defaults.
        /// </summary>
        private void ProcessPendingDamage()
        {
            if (_context == null) return;

            foreach (var kvp in _context.PendingDamage)
            {
                int targetId = kvp.Key;
                float damage = kvp.Value.damage;
                int? instigatorId = kvp.Value.instigator;

                if (!_entities.TryGetValue(targetId, out var target)) continue;

                var config = _registry?.GetConfig(target.EntityType);
                if (config == null || !config.CanTakeDamage) continue;

                // Apply damage multiplier
                damage *= config.DamageReceivedMultiplier;

                // For ships with extracted capabilities, use module-derived values
                if (target.EntityType == EntityType.Ship && target.HasShipCapabilities())
                {
                    var caps = target.GetShipCapabilities();

                    // Shield absorption using module values
                    if (caps.HasShields && caps.CurrentShield > 0f)
                    {
                        float shieldDamage = Mathf.Min(caps.CurrentShield, damage);
                        caps.CurrentShield -= shieldDamage;
                        damage -= shieldDamage;
                        target.SetTypeData("LastShieldDamageTime", _context.CurrentTime);
                    }

                    // Hull damage using module values
                    if (damage > 0f)
                    {
                        caps.CurrentIntegrity -= damage;

                        if (caps.CurrentIntegrity <= 0f)
                        {
                            _destructionBuffer.Add(targetId);
                            RecordKillingBlowEvent(target, instigatorId, kvp.Value.damage);
                        }
                    }

                    target.SetShipCapabilities(caps);
                    continue;
                }

                // Fallback: Use config-based values for non-ships or ships without capabilities
                if (config.HasShields)
                {
                    float shields = target.GetTypeData("CurrentShields", 0f);
                    if (shields > 0f)
                    {
                        float shieldDamage = Mathf.Min(shields, damage);
                        shields -= shieldDamage;
                        damage -= shieldDamage;
                        target.SetTypeData("CurrentShields", shields);
                        target.SetTypeData("LastShieldDamageTime", _context.CurrentTime);
                    }
                }

                // Apply remaining damage to health
                if (damage > 0f)
                {
                    float health = target.GetTypeData("CurrentHealth", config.DefaultHealth);
                    health -= damage;
                    target.SetTypeData("CurrentHealth", health);

                    if (health <= 0f)
                    {
                        _destructionBuffer.Add(targetId);
                        RecordKillingBlowEvent(target, instigatorId, kvp.Value.damage);
                    }
                }
            }
        }

        /// <summary>
        /// Record a killing blow combat event.
        /// </summary>
        private void RecordKillingBlowEvent(SimulatedEntity target, int? instigatorId, float damage)
        {
            _context.RecordCombatEvent(new CombatEventData
            {
                AttackerId = instigatorId ?? -1,
                TargetId = target.EntityId,
                Damage = damage,
                Position = target.AbsolutePosition,
                DamageType = CombatDamageType.Weapon,
                WasKillingBlow = true
            });
        }

        /// <summary>
        /// Legacy physics update for backward compatibility.
        /// </summary>
        private void UpdatePhysicsLegacy(SimulatedEntity entity, float dt)
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
