using Unity.Entities;

namespace Starfire.Entity
{
    public struct ShipPropulsion : IComponentData
    {
        public int ConfigId;
        public float CurrentHealth;
        public float MaxHealth;
        public float MaxSpeed;
        public float Acceleration;
        public float DragCoefficient;
        public byte IsEnabled;
    }
}