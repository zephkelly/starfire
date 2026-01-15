using Starfire.Entity.Modules.Damage;
using Starfire.Entity.Modules.Shield;
using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Shared utility for projectile raycasting and damage application.
    /// Used by RaycastProjectile and HitscanProjectile modes.
    /// </summary>
    public static class ProjectileRaycastUtility
    {
        /// <summary>
        /// Result of a projectile raycast including hit information and component references.
        /// </summary>
        public struct RaycastHitResult
        {
            /// <summary>Whether the raycast hit anything.</summary>
            public bool DidHit;

            /// <summary>World position of the hit point.</summary>
            public Vector2 HitPoint;

            /// <summary>Surface normal at the hit point.</summary>
            public Vector2 HitNormal;

            /// <summary>Distance from ray origin to hit point.</summary>
            public float Distance;

            /// <summary>The collider that was hit.</summary>
            public Collider2D HitCollider;

            /// <summary>The damage receiver on the hit entity. May be null.</summary>
            public IDamageReceiver DamageReceiver;

            /// <summary>The shield boundary if a shield was hit. Null if hull was hit directly.</summary>
            public ShieldBoundary ShieldBoundary;

            /// <summary>The shield visual for ripple effects. May be null.</summary>
            public ShieldVisual ShieldVisual;

            /// <summary>The shield module for accessing shield config. May be null.</summary>
            public IShieldShipModule ShieldModule;

            /// <summary>The entity controller that owns the hit target.</summary>
            public EntityControllerBase TargetController;
        }

        /// <summary>
        /// Performs a raycast that properly handles shields and entities.
        /// Returns the first valid hit (shield or entity, excluding owner).
        /// </summary>
        /// <param name="origin">Ray starting position in world space.</param>
        /// <param name="direction">Normalized ray direction.</param>
        /// <param name="maxDistance">Maximum ray distance.</param>
        /// <param name="hitLayers">Layer mask for collision detection.</param>
        /// <param name="owner">The entity that fired the projectile (excluded from hits).</param>
        /// <param name="bypassShields">If true, skip shield colliders and hit hull directly.</param>
        public static RaycastHitResult Raycast(
            Vector2 origin,
            Vector2 direction,
            float maxDistance,
            LayerMask hitLayers,
            EntityControllerBase owner,
            bool bypassShields = false)
        {
            // Use RaycastAll to handle multiple potential hits (shields in front of hulls)
            var hits = Physics2D.RaycastAll(origin, direction, maxDistance, hitLayers);

            // Sort by distance to process nearest first
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // Skip hits at the origin (distance ~0)
                if (hit.distance < 0.001f) continue;

                // Get the entity controller for owner check
                var targetController = hit.collider.GetComponentInParent<EntityControllerBase>();

                // Skip owner - don't hit the entity that fired the projectile
                if (targetController != null && targetController == owner) continue;

                // Check for shield boundary
                var shieldBoundary = hit.collider.GetComponent<ShieldBoundary>();

                if (shieldBoundary != null)
                {
                    // Skip shields if bypassing
                    if (bypassShields) continue;

                    // Get references directly from ShieldBoundary - these are set during Initialize()
                    // This is more reliable than GetComponentInParent lookups on dynamically created objects
                    return new RaycastHitResult
                    {
                        DidHit = true,
                        HitPoint = hit.point,
                        HitNormal = hit.normal,
                        Distance = hit.distance,
                        HitCollider = hit.collider,
                        DamageReceiver = shieldBoundary.DamageReceiver,
                        ShieldBoundary = shieldBoundary,
                        ShieldVisual = shieldBoundary.ShieldVisual,
                        ShieldModule = shieldBoundary.ShieldModule,
                        TargetController = shieldBoundary.OwnerController
                    };
                }

                // Regular hit (hull, asteroid, etc.)
                // Try multiple approaches to find the damage receiver
                IDamageReceiver hullDamageReceiver = hit.collider.GetComponentInParent<IDamageReceiver>();
                if (hullDamageReceiver == null && targetController != null)
                {
                    // Fallback: get from the entity controller directly
                    hullDamageReceiver = targetController.GetComponent<IDamageReceiver>();
                }

                return new RaycastHitResult
                {
                    DidHit = true,
                    HitPoint = hit.point,
                    HitNormal = hit.normal,
                    Distance = hit.distance,
                    HitCollider = hit.collider,
                    DamageReceiver = hullDamageReceiver,
                    ShieldBoundary = null,
                    ShieldVisual = null,
                    ShieldModule = null,
                    TargetController = targetController
                };
            }

            // No valid hit found
            return new RaycastHitResult { DidHit = false };
        }

        /// <summary>
        /// Applies damage at a raycast hit point.
        /// Handles shield vs hull damage correctly and spawns appropriate effects.
        /// </summary>
        /// <param name="hitResult">The raycast hit result.</param>
        /// <param name="damage">Base damage value.</param>
        /// <param name="damageConfig">Damage type configuration.</param>
        /// <param name="source">The entity that fired the projectile.</param>
        /// <param name="direction">Normalized projectile travel direction.</param>
        /// <param name="impactConfig">Visual/audio effect configuration.</param>
        /// <param name="projectileVelocity">Velocity for reflection particle calculations.</param>
        public static void ApplyDamage(
            RaycastHitResult hitResult,
            float damage,
            WeaponDamageConfig damageConfig,
            EntityControllerBase source,
            Vector2 direction,
            ImpactConfig impactConfig,
            Vector2 projectileVelocity = default)
        {
            if (!hitResult.DidHit) return;

            var config = damageConfig ?? WeaponDamageConfig.Default;

            // Apply damage if receiver exists and can receive damage
            if (hitResult.DamageReceiver != null && hitResult.DamageReceiver.CanReceiveDamage)
            {
                var damageInfo = new DamageInfo(
                    baseDamage: damage,
                    type: config.damageType,
                    shieldMultiplier: config.shieldDamageMultiplier,
                    hullMultiplier: config.hullDamageMultiplier,
                    source: source,
                    sourcePosition: hitResult.HitPoint,
                    direction: hitResult.ShieldBoundary != null ? hitResult.HitNormal : direction,
                    bypassesShield: config.bypassesShield,
                    shieldPenetration: config.shieldPenetration
                );

                hitResult.DamageReceiver.ReceiveDamage(damageInfo);
            }

            // Spawn effects based on hit type
            if (hitResult.ShieldBoundary != null)
            {
                SpawnShieldImpactEffects(hitResult, impactConfig, direction, projectileVelocity);
            }
            else
            {
                SpawnHullImpactEffects(hitResult, impactConfig, direction);
            }
        }

        private static void SpawnShieldImpactEffects(
            RaycastHitResult hitResult,
            ImpactConfig impactConfig,
            Vector2 direction,
            Vector2 projectileVelocity)
        {
            // Get ship velocity for effect inheritance
            Vector2 shipVelocity = hitResult.TargetController?.Rigidbody?.linearVelocity ?? Vector2.zero;

            // Spawn shield-specific impact effect if configured
            if (hitResult.ShieldModule?.ShieldImpactConfig != null)
            {
                ShieldImpactEffect.Spawn(
                    hitResult.ShieldModule.ShieldImpactConfig,
                    hitResult.HitPoint,
                    hitResult.HitNormal,
                    shipVelocity
                );
            }

            // Spawn projectile impact effect with reflection particles
            if (impactConfig != null)
            {
                // Use projectile velocity if provided, otherwise use direction-based velocity
                Vector2 incomingVelocity = projectileVelocity.sqrMagnitude > 0.01f
                    ? projectileVelocity
                    : direction * 100f; // Fallback velocity for reflection calc

                ImpactEffect.Spawn(
                    impactConfig,
                    hitResult.HitPoint,
                    -hitResult.HitNormal,
                    incomingVelocity,
                    hitResult.HitNormal
                );
            }

            // Notify shield visual for ripple effect
            if (hitResult.ShieldVisual != null)
            {
                hitResult.ShieldVisual.RegisterImpact(hitResult.HitPoint);
            }
        }

        private static void SpawnHullImpactEffects(
            RaycastHitResult hitResult,
            ImpactConfig impactConfig,
            Vector2 direction)
        {
            // Spawn standard impact effect
            if (impactConfig != null)
            {
                ImpactEffect.Spawn(impactConfig, hitResult.HitPoint, direction);
            }
        }
    }
}
