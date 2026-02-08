using UnityEngine;

namespace Starfire.Entity
{
    public enum ShipHullType
    {
        Light = 0,
        Medium = 1,
        Heavy = 2,
        Reinforced = 3
    }

    // Burstable data structure
    public struct ShipHullData
    {
        public int ConfigId;
        public float CurrentHealth;
        public float CurrentTemperature;
        public byte IsEnabled;
    }
    // Reference configuration for burstable modules
    public struct ShipHullConfigData
    {
        public int ConfigId;
        public float MaxHealth;
        public float MaxTemperature;
        public float EfficiencyCoefficient;
    }

    [CreateAssetMenu(fileName = "Hull Config", menuName = "Starfire/Ship/Modules/Hull")]
    public class ShipHullModuleConfig : ScriptableObject
    {
        public int ConfigId;
        public string ModuleId;
        public string DisplayName;

        public float MaxHealth;
        public float MaxTemperature;
        public float EfficiencyCoefficient;

        public Sprite Icon;
        public GameObject VisualPrefab;
        public Material Material;

        public ShipHullType HullType;

        public ShipHullConfigData ToBurstData()
        {
            return new ShipHullConfigData
            {
                ConfigId = ConfigId,
                MaxHealth = MaxHealth,
                MaxTemperature = MaxTemperature,
                EfficiencyCoefficient = EfficiencyCoefficient
            };
        }
    }

    // OOP implementation of a Hull Module
    // public class ShipHullModule : IShipModule
    // {
    //     public string ModuleId { get; set; }
    //     public ShipModuleCategory Category => ShipModuleCategory.Hull;

    //     public float MaxHealth { get; set; }
    //     public float CurrentHealth { get; set; }
    //     public bool IsEnabled { get; set; } = true;

    //     public float MaxTemperature { get; set; }
    //     public float CurrentTemperature { get; set; }

    //     public float HealthCoefficient => CurrentHealth / MaxHealth;
    //     public float EfficiencyMultiplier { get; set; } = 1.0f;

    //     public void Damage(float amount)
    //     {
    //         float effectiveDamage = amount * EfficiencyMultiplier;
    //         CurrentHealth -= effectiveDamage;
    //         if (CurrentHealth < 0)
    //         {
    //             CurrentHealth = 0;
    //             IsEnabled = false;
    //         }
    //     }

    //     public void Repair(float amount)
    //     {
    //         CurrentHealth += amount;
    //         if (CurrentHealth > MaxHealth)
    //         {
    //             CurrentHealth = MaxHealth;
    //         }
    //     }

    //     public void Update(float deltaTime) { }
    // }
}