using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Simulation.Behaviors;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation.Config
{
    /// <summary>
    /// Collision response type for simulated entities.
    /// </summary>
    public enum CollisionResponseType
    {
        /// <summary>Bounce off with elastic collision.</summary>
        Bounce,
        /// <summary>Take damage based on impact energy.</summary>
        Damage,
        /// <summary>Destroy on any collision.</summary>
        Destroy,
        /// <summary>No collision response (pass through).</summary>
        None
    }

    /// <summary>
    /// ScriptableObject that defines how a specific entity type behaves in background simulation.
    /// Create one asset per entity type (Asteroid, Ship, Projectile, Missile, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "NewEntityTypeConfig", menuName = "Starfire/Simulation/Entity Type Config")]
    public class SimulationEntityTypeConfig : ScriptableObject
    {
        // ── Identity ─────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("The entity type this config applies to.")]
        public EntityType EntityType;

        [Tooltip("Human-readable name for editor display.")]
        public string DisplayName;

        [TextArea(2, 4)]
        [Tooltip("Description of this entity type's simulation behavior.")]
        public string Description;

        // ── Simulation Tier Settings ─────────────────────────────────────────

        [Header("Simulation Tier Settings")]
        [Tooltip("Higher priority entities stay in Tier 1 longer before demotion to Tier 2. Range: 0-100.")]
        [Range(0, 100)]
        public int Tier1Priority = 50;

        [Tooltip("If true, this entity type can be predicted analytically in Tier 2. If false, stays in Tier 1.")]
        public bool AllowTier2Prediction = true;

        [Tooltip("Minimum distance (in chunks) before this entity can be demoted to Tier 2.")]
        [Min(1)]
        public int MinTier2Distance = 3;

        // ── Behaviors ────────────────────────────────────────────────────────

        [Header("Behaviors")]
        [Tooltip("The default behavior when no specific behavior is assigned.")]
        public SimulationBehaviorConfig DefaultBehavior;

        [Tooltip("List of all behaviors this entity type can use.")]
        public List<SimulationBehaviorConfig> AvailableBehaviors = new();

        // ── Physics ──────────────────────────────────────────────────────────

        [Header("Physics")]
        [Tooltip("Apply drag to velocity each frame.")]
        public bool UseDrag = true;

        [Tooltip("Default drag coefficient if not specified by entity.")]
        [Min(0f)]
        public float DefaultDrag = 0.1f;

        [Tooltip("Whether this entity participates in collision detection.")]
        public bool CanCollide = true;

        [Tooltip("How this entity responds to collisions.")]
        public CollisionResponseType CollisionResponse = CollisionResponseType.Bounce;

        [Tooltip("Collision elasticity (0 = inelastic, 1 = perfectly elastic).")]
        [Range(0f, 1f)]
        public float CollisionElasticity = 0.8f;

        // ── Combat ───────────────────────────────────────────────────────────

        [Header("Combat")]
        [Tooltip("Whether this entity can receive damage in simulation.")]
        public bool CanTakeDamage = true;

        [Tooltip("Whether this entity can deal damage to others.")]
        public bool CanDealDamage = false;

        [Tooltip("Multiplier applied to damage dealt by this entity.")]
        [Min(0f)]
        public float DamageMultiplier = 1f;

        [Tooltip("Multiplier applied to damage received by this entity.")]
        [Min(0f)]
        public float DamageReceivedMultiplier = 1f;

        [Tooltip("Base health for entities without explicit health.")]
        [Min(0f)]
        public float DefaultHealth = 100f;

        // ── Shields ──────────────────────────────────────────────────────────

        [Header("Shields")]
        [Tooltip("Whether this entity type has shields.")]
        public bool HasShields = false;

        [Tooltip("Whether shields regenerate while in background simulation.")]
        public bool RegenerateShieldsInSimulation = true;

        [Tooltip("Shield regeneration rate per second.")]
        [Min(0f)]
        public float ShieldRegenRate = 5f;

        [Tooltip("Delay in seconds before shields start regenerating after taking damage.")]
        [Min(0f)]
        public float ShieldRegenDelay = 2f;

        [Tooltip("Default max shield value for entities without explicit shields.")]
        [Min(0f)]
        public float DefaultMaxShields = 50f;

        // ── Lifetime ─────────────────────────────────────────────────────────

        [Header("Lifetime")]
        [Tooltip("Whether this entity has a limited lifetime (e.g., projectiles).")]
        public bool HasLifetime = false;

        [Tooltip("Default lifetime in seconds for entities with limited lifetime.")]
        [Min(0f)]
        public float DefaultLifetime = 10f;

        [Tooltip("What happens when lifetime expires.")]
        public LifetimeExpiredAction OnLifetimeExpired = LifetimeExpiredAction.Destroy;

        // ── Spawning ─────────────────────────────────────────────────────────

        [Header("Spawning")]
        [Tooltip("Prefab to spawn when this entity is promoted back to a GameObject.")]
        public GameObject SpawnPrefab;

        [Tooltip("Whether to use object pooling for this entity type.")]
        public bool UsePooling = true;

        [Tooltip("Initial pool size for this entity type.")]
        [Min(0)]
        public int InitialPoolSize = 10;

        // ── Validation ───────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(DisplayName))
            {
                DisplayName = EntityType.ToString();
            }

            // Ensure default behavior is in available behaviors list
            if (DefaultBehavior != null && !AvailableBehaviors.Contains(DefaultBehavior))
            {
                AvailableBehaviors.Insert(0, DefaultBehavior);
            }
        }

        /// <summary>
        /// Check if this entity type supports a specific behavior.
        /// </summary>
        public bool SupportsBehavior(SimulationBehaviorConfig behavior)
        {
            if (behavior == null) return false;
            return AvailableBehaviors.Contains(behavior) || behavior == DefaultBehavior;
        }

        /// <summary>
        /// Get a behavior by type from available behaviors.
        /// </summary>
        public SimulationBehaviorConfig GetBehaviorByType(SimulatedBehaviorType type)
        {
            foreach (var behavior in AvailableBehaviors)
            {
                if (behavior != null && behavior.BehaviorType == type)
                    return behavior;
            }

            // Check default behavior
            if (DefaultBehavior != null && DefaultBehavior.BehaviorType == type)
                return DefaultBehavior;

            return null;
        }
    }

    /// <summary>
    /// What happens when an entity's lifetime expires.
    /// </summary>
    public enum LifetimeExpiredAction
    {
        /// <summary>Entity is destroyed/removed from simulation.</summary>
        Destroy,
        /// <summary>Entity stops and becomes stationary.</summary>
        Stop,
        /// <summary>Entity continues but is marked as expired (for custom handling).</summary>
        MarkExpired
    }
}
