using Unity.Entities;

namespace Starfire.Entity
{
    public struct ShipHull : IComponentData
    {
        public int ConfigId;
        public float CurrentHealth;
        public float MaxHealth;
        public float CurrentTemperature;
        public float MaxTemperature;
        public float EfficiencyCoefficient;
        public byte HullType;
        public byte IsEnabled;
    }
}