using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject configuration for point defense modules.
    /// Create assets via: Create > StarfireV2 > Modules > Weapon > PointDefense
    /// </summary>
    [CreateAssetMenu(fileName = "PointDefense", menuName = "StarfireV2/Modules/Weapon/PointDefense")]
    public class PointDefenseModuleConfig : ScriptableObject, IShipModuleConfig
    {
        [Header("Module Identity")]
        [Tooltip("Unique identifier for this module.")]
        [SerializeField] private string moduleId;

        [Tooltip("Display name shown in UI.")]
        [SerializeField] private string displayName;

        [Header("Weight Class")]
        [Tooltip("The weight class of this weapon. Point defense is typically Light.")]
        [SerializeField] private WeaponWeightClass weightClass = WeaponWeightClass.Light;

        [Header("Weapon Stats")]
        [Tooltip("Damage dealt per shot.")]
        [SerializeField] private float damage = 5f;

        [Tooltip("Fire rate in shots per second.")]
        [SerializeField] private float fireRate = 5f;

        [Tooltip("Maximum firing range.")]
        [SerializeField] private float range = 10f;

        [Header("Point Defense Settings")]
        [Tooltip("Maximum range at which threats are detected and tracked.")]
        [SerializeField] private float engagementRange = 15f;

        [Tooltip("Tracking speed in degrees per second.")]
        [SerializeField] private float trackingSpeed = 360f;

        [Tooltip("Maximum number of targets to track simultaneously.")]
        [SerializeField] private int maxTrackedTargets = 3;

        [Tooltip("Layers that are considered threats (projectiles, missiles, etc.)")]
        [SerializeField] private LayerMask threatLayers;

        [Tooltip("How often to scan for new threats (seconds).")]
        [SerializeField] private float scanInterval = 0.1f;

        [Header("Accuracy")]
        [Tooltip("Base accuracy percentage (0-100). At 100%, shots are perfectly aimed. At 0%, shots deviate by max spread angle.")]
        [Range(0f, 100f)]
        [SerializeField] private float accuracyPercent = 72f;

        [Tooltip("Maximum angular deviation in degrees when accuracy is 0%.")]
        [SerializeField] private float maxSpreadAngle = 15f;

        [Tooltip("Whether accuracy decreases with distance to target.")]
        [SerializeField] private bool accuracyDecayOverRange = false;

        [Tooltip("Curve defining how accuracy falls off with range (0 = point blank, 1 = max range). Only used if accuracyDecayOverRange is true.")]
        [SerializeField] private AnimationCurve rangeAccuracyFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.8f);

        [Header("Sensor Integration")]
        [Tooltip("Whether to use sensor module for threat detection. Falls back to direct physics if no sensor is available.")]
        [SerializeField] private bool useSensorIntegration = true;

        [Header("Turret Settings")]
        [Tooltip("Configuration for turret rotation. Point defense is typically always a turret.")]
        [SerializeField] private V2TurretSettings turretSettings;

        [Header("Projectile")]
        [Tooltip("Configuration for point defense projectile behavior.")]
        [SerializeField] private V2ProjectileConfig projectileConfig;

        [Tooltip("Prefab to instantiate for Physics mode projectiles.")]
        [SerializeField] private GameObject projectilePrefab;

        [Header("Visual")]
        [Tooltip("Prefab for the point defense visual instantiated at hardpoint.")]
        [SerializeField] private GameObject weaponVisualPrefab;

        [Header("Screen Shake")]
        [Tooltip("Configuration for camera screen shake when firing.")]
        [SerializeField] private V2FireShakeConfig fireShakeConfig;

        // Public accessors
        public string ModuleId => moduleId;
        public string DisplayName => displayName;
        public WeaponWeightClass WeightClass => weightClass;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public float EngagementRange => engagementRange;
        public float TrackingSpeed => trackingSpeed;
        public int MaxTrackedTargets => maxTrackedTargets;
        public LayerMask ThreatLayers => threatLayers;
        public float ScanInterval => scanInterval;
        public float AccuracyPercent => accuracyPercent;
        public float MaxSpreadAngle => maxSpreadAngle;
        public bool AccuracyDecayOverRange => accuracyDecayOverRange;
        public AnimationCurve RangeAccuracyFalloff => rangeAccuracyFalloff;
        public bool UseSensorIntegration => useSensorIntegration;
        public V2TurretSettings TurretSettings => turretSettings ?? GetDefaultTurretSettings();
        public V2ProjectileConfig ProjectileConfig => projectileConfig;
        public GameObject ProjectilePrefab => projectilePrefab;
        public GameObject WeaponVisualPrefab => weaponVisualPrefab;
        public V2FireShakeConfig FireShakeConfig => fireShakeConfig;

        // IShipModuleConfig implementation
        public ShipModuleTypeId TypeId => ShipModuleTypeId.PointDefense;

        public IShipModule CreateModule()
        {
            return new PointDefenseModule(this);
        }

        private static V2TurretSettings GetDefaultTurretSettings()
        {
            return new V2TurretSettings
            {
                isTurret = true,
                rotationSpeed = 360f,
                firingArc = 360f,
                canFireWhileRotating = true,
                firingTolerance = 10f,
                predictTargetPosition = true
            };
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(moduleId))
            {
                moduleId = name;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }

            if (damage < 0) damage = 0;
            if (fireRate <= 0) fireRate = 0.1f;
            if (range <= 0) range = 1f;
            if (engagementRange < range) engagementRange = range;
            if (trackingSpeed <= 0) trackingSpeed = 1f;
            if (maxTrackedTargets <= 0) maxTrackedTargets = 1;
            if (scanInterval <= 0) scanInterval = 0.1f;
            if (maxSpreadAngle < 0) maxSpreadAngle = 0f;
            if (maxSpreadAngle > 90f) maxSpreadAngle = 90f;

            // Ensure falloff curve exists with sensible defaults
            if (rangeAccuracyFalloff == null || rangeAccuracyFalloff.keys.Length == 0)
            {
                rangeAccuracyFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0.8f);
            }
        }
    }
}
