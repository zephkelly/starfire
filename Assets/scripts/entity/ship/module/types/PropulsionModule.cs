using UnityEngine;

namespace Starfire.Entity
{
    public struct ShipPropulsionData
    {
        public int ConfigId;
        public float CurrentHealth;
        public byte IsEnabled;
    }

    public struct ShipPropulsionConfigData
    {
        public int ConfigId;
        public float MaxHealth;
        public float MaxSpeed;
        public float Acceleration;
        public float DragCoefficient;
    }

    [CreateAssetMenu(fileName = "Propulsion Config", menuName = "Starfire/Ship/Modules/Propulsion")]
    public class ShipPropulsionModuleConfig : ScriptableObject
    {
        public int ConfigId;
        public string ModuleId;
        public string DisplayName;
        
        public float MaxHealth;
        public float MaxSpeed;
        public float Acceleration;
        public float DragCoefficient;

        public Sprite Icon;
        public GameObject VisualPrefab;
        public Material Material;

        public ShipPropulsionConfigData ToBurstData()
        {
            return new ShipPropulsionConfigData
            {
                ConfigId = ConfigId,
                MaxHealth = MaxHealth,
                MaxSpeed = MaxSpeed,
                Acceleration = Acceleration,
                DragCoefficient = DragCoefficient
            };
        }
    }

    // public class ShipPropulsionModule : IShipModule
    // {
    //     public string ModuleId { get; private set; }
    //     public ShipModuleCategory Category => ShipModuleCategory.Propulsion;

    //     public float MaxHealth { get; private set; }
    //     public float CurrentHealth { get; set; }
    //     public float HealthCoefficient => CurrentHealth / MaxHealth;
    //     public float EfficiencyMultiplier => HealthCoefficient;
    //     public bool IsEnabled { get; set; } = true;

    //     public float MaxSpeed { get; set; }
    //     public float Acceleration { get; set; }
    //     public float CurrentDrag { get; set; } = 0.0f;

    //     public void Damage(float amount)
    //     {
    //         if (!IsEnabled) return;
            
    //         CurrentHealth -= amount;
    //         if (CurrentHealth < 0) CurrentHealth = 0;
    //     }
        
    //     public void Repair(float amount)
    //     {
    //         CurrentHealth += amount;
    //         if (CurrentHealth > MaxHealth)
    //             CurrentHealth = MaxHealth;
    //     }
    // }
}