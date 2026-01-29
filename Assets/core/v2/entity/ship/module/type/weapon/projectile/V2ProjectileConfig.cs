using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject configuration for projectile behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileConfig", menuName = "StarfireV2/Weapon/ProjectileConfig")]
    public class V2ProjectileConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Speed of the projectile in units per second.")]
        public float speed = 20f;

        [Tooltip("Maximum lifetime in seconds before auto-destroy.")]
        public float lifetime = 3f;

        [Tooltip("Whether to add the ship's velocity to the projectile velocity.")]
        public bool inheritVelocity = true;

        [Header("Collision")]
        [Tooltip("Layers that the projectile can hit.")]
        public LayerMask hitLayers = ~0;

        [Tooltip("Whether the projectile destroys itself on first hit.")]
        public bool destroyOnHit = true;

        [Tooltip("Number of targets the projectile can penetrate before being destroyed. 0 = no penetration.")]
        public int maxPenetrations = 0;

        [Header("Visual")]
        [Tooltip("Scale multiplier for the projectile sprite.")]
        public float scale = 1f;

        [Tooltip("Tint color for the projectile.")]
        public Color color = Color.white;

        [Tooltip("Optional trail renderer prefab to attach (legacy/advanced use).")]
        public GameObject trailPrefab;

        [Tooltip("Inline shader-based trail configuration. Used when trailPrefab is null and enabled.")]
        public V2TrailConfig trailConfig;

        [Header("Mode")]
        [Tooltip("How the projectile handles collision detection and movement.")]
        public V2ProjectileMode mode = V2ProjectileMode.Physics;

        [Header("Impact")]
        [Tooltip("Configuration for impact effects when the projectile hits something.")]
        public V2ImpactConfig impactConfig;

        [Header("Hitscan Settings")]
        [Tooltip("Duration the hitscan beam visual is shown.")]
        public float hitscanDuration = 0.1f;

        [Tooltip("Width of the hitscan beam visual.")]
        public float hitscanWidth = 0.1f;

        [Tooltip("Color of the hitscan beam.")]
        public Color hitscanColor = Color.red;

        [Tooltip("Material for the hitscan line renderer.")]
        public Material hitscanMaterial;

        [Header("Missile Settings")]
        [Tooltip("Configuration for missile homing and weaving behavior. Only used when mode is Missile.")]
        public V2MissileConfig missileConfig;

        [Header("Thrust Missile Settings")]
        [Tooltip("Configuration for thrust-based missile behavior. Only used when mode is ThrustMissile.")]
        public V2ThrustMissileConfig thrustMissileConfig;
    }
}
