using System;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class OffensiveWeaponModuleData : IModuleRuntimeData
    {
        public string moduleId;
        public string displayName;
        public WeaponWeightClass weightClass = WeaponWeightClass.Medium;
        public float damage = 10f;
        public float fireRate = 1f;
        public float range = 20f;

        // Asset references (not typically overridden, but available)
        public V2WeaponDamageConfig damageConfig;
        public V2TurretSettings turretSettings;
        public V2ProjectileConfig projectileConfig;
        public GameObject projectilePrefab;
        public GameObject weaponVisualPrefab;
        public V2FireShakeConfig fireShakeConfig;

        public string ModuleId => moduleId;
        public ShipModuleTypeId TypeId => ShipModuleTypeId.Weapon;
    }
}
