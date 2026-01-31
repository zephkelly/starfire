using System;
using UnityEngine;

namespace StarfireV2
{
    [Serializable]
    public class PointDefenseModuleData : IModuleRuntimeData
    {
        public string moduleId;
        public string displayName;
        public WeaponWeightClass weightClass = WeaponWeightClass.Light;

        [Header("Weapon Stats")]
        public float damage = 5f;
        public float fireRate = 5f;
        public float range = 10f;

        [Header("Point Defense")]
        public float engagementRange = 15f;
        public float trackingSpeed = 360f;
        public int maxTrackedTargets = 5;
        public LayerMask threatLayers;
        public float scanInterval = 0.2f;

        [Header("Accuracy")]
        [Range(0f, 100f)]
        public float accuracyPercent = 85f;
        public float maxSpreadAngle = 5f;
        public bool accuracyDecayOverRange;
        public AnimationCurve rangeAccuracyFalloff;

        [Header("Burst Fire")]
        public bool useBurstFire;
        public int burstCount = 5;
        public float burstInterval = 0.05f;
        public float burstCooldown = 2f;

        [Header("Collision Course")]
        public float collisionCourseRadius = 2f;

        [Header("Targeting")]
        public PointDefenseTargetingMode targetingMode = PointDefenseTargetingMode.Auto;
        public bool useSensorIntegration = true;

        // Asset references
        public V2TurretSettings turretSettings;
        public V2ProjectileConfig projectileConfig;
        public GameObject projectilePrefab;
        public GameObject weaponVisualPrefab;
        public V2FireShakeConfig fireShakeConfig;

        public string ModuleId => moduleId;
        public ShipModuleTypeId TypeId => ShipModuleTypeId.PointDefense;
    }
}
