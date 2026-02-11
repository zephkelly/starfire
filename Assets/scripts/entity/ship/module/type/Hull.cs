using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct ShipHull : IComponentData
    {
        [GhostField] public int ConfigId;
        [GhostField(Quantization = 100)] public float CurrentHealth;
        [GhostField(Quantization = 100)] public float MaxHealth;
        public float CurrentTemperature;
        public float MaxTemperature;
        public float EfficiencyCoefficient;
        [GhostField] public byte HullType;
        public byte IsEnabled;
    }
}