using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject configuration for offensive weapon modules.
    /// Create assets via: Create > StarfireV2 > Modules > Weapon > Offensive
    /// </summary>
    [CreateAssetMenu(fileName = "OffensiveWeapon", menuName = "StarfireV2/Modules/Weapon/Offensive")]
    public class OffensiveWeaponModuleConfig : ScriptableObject, IShipModuleConfig
    {
        [Header("Module Identity")]
        [Tooltip("Unique identifier for this module.")]
        [SerializeField] private string moduleId;

        [Tooltip("Display name shown in UI.")]
        [SerializeField] private string displayName;

        [Header("Weight Class")]
        [Tooltip("The weight class of this weapon. Determines which hardpoints it can mount on.")]
        [SerializeField] private WeaponWeightClass weightClass = WeaponWeightClass.Medium;

        [Header("Weapon Stats")]
        [Tooltip("Base damage dealt per shot.")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Fire rate in shots per second.")]
        [SerializeField] private float fireRate = 1f;

        [Tooltip("Maximum effective range.")]
        [SerializeField] private float range = 20f;

        [Header("Damage Configuration")]
        [Tooltip("Configuration for damage types and multipliers.")]
        [SerializeField] private V2WeaponDamageConfig damageConfig;

        [Header("Turret Settings")]
        [Tooltip("Configuration for turret rotation behavior.")]
        [SerializeField] private V2TurretSettings turretSettings;

        [Header("Projectile")]
        [Tooltip("Configuration for projectile behavior.")]
        [SerializeField] private V2ProjectileConfig projectileConfig;

        [Tooltip("Prefab to instantiate for Physics mode projectiles.")]
        [SerializeField] private GameObject projectilePrefab;

        [Header("Visual")]
        [Tooltip("Prefab for the weapon visual (turret, barrel, etc.) instantiated at hardpoint.")]
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
        public V2WeaponDamageConfig DamageConfig => damageConfig ?? V2WeaponDamageConfig.Default;
        public V2TurretSettings TurretSettings => turretSettings ?? V2TurretSettings.Fixed;
        public V2ProjectileConfig ProjectileConfig => projectileConfig;
        public GameObject ProjectilePrefab => projectilePrefab;
        public GameObject WeaponVisualPrefab => weaponVisualPrefab;
        public V2FireShakeConfig FireShakeConfig => fireShakeConfig;

        // IShipModuleConfig implementation
        public ShipModuleTypeId TypeId => ShipModuleTypeId.Weapon;

        public IShipModule CreateModule()
        {
            return new OffensiveWeaponModule(this);
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
        }
    }
}
