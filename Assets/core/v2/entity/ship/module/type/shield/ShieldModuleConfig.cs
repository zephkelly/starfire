using UnityEngine;

namespace StarfireV2
{
    [CreateAssetMenu(fileName = "ShieldModule", menuName = "StarfireV2/Modules/Shield")]
    public class ShieldModuleConfig : ScriptableObject, IShipModuleConfig
    {
        public ShipModuleTypeId TypeId => ShipModuleTypeId.Shield;

        [Header("Module Identity")]
        [SerializeField] private string moduleId = "shield_module";

        [Header("Shield Stats")]
        [SerializeField] private int maxShield = 50;
        [SerializeField] private float regenRate = 5f;

        [Header("Recharge Settings")]
        [Tooltip("Seconds to wait after taking damage before regeneration starts")]
        [SerializeField] private float rechargeDelay = 3f;

        [Header("Damage Resistances")]
        [Tooltip("Optional: Per-damage-type resistances for this shield")]
        [SerializeField] private DamageResistances damageResistances;

        [Header("Shield Boundary")]
        [Tooltip("Enable a physical boundary collider for the shield")]
        [SerializeField] private bool enableBoundary = true;

        [Tooltip("Size of the elliptical boundary (x = horizontal radius, y = vertical radius)")]
        [SerializeField] private Vector2 boundarySize = new Vector2(2f, 1.5f);

        [Tooltip("Offset of the boundary center from the entity origin")]
        [SerializeField] private Vector2 boundaryOffset = Vector2.zero;

        [Tooltip("Number of vertices used to approximate the ellipse (higher = smoother)")]
        [SerializeField, Range(8, 64)] private int boundaryResolution = 24;

        [Header("Shield Impact")]
        [Tooltip("Impact effect configuration for shield hits")]
        [SerializeField] private ShieldImpactConfig shieldImpactConfig;

        [Header("Shield Visual")]
        [Tooltip("Visual appearance configuration for the shield barrier")]
        [SerializeField] private ShieldVisualConfig visualConfig;

        // Accessors for ShieldModule constructor
        public DamageResistances DamageResistances => damageResistances;
        public ShieldImpactConfig ShieldImpactConfig => shieldImpactConfig;
        public ShieldVisualConfig VisualConfig => visualConfig;

        public IModuleRuntimeData ToData()
        {
            return new ShieldModuleData
            {
                moduleId = moduleId,
                maxShield = maxShield,
                regenRate = regenRate,
                rechargeDelay = rechargeDelay,
                enableBoundary = enableBoundary,
                boundarySize = boundarySize,
                boundaryOffset = boundaryOffset,
                boundaryResolution = boundaryResolution
            };
        }

        public IShipModule CreateModule()
        {
            return new ShieldModule((ShieldModuleData)ToData(), damageResistances, shieldImpactConfig, visualConfig);
        }

        public IShipModule CreateModuleFromData(IModuleRuntimeData data)
        {
            return new ShieldModule((ShieldModuleData)data, damageResistances, shieldImpactConfig, visualConfig);
        }
    }
}
