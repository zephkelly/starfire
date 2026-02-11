using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct ShipPropulsion : IComponentData
    {
        [GhostField] public int ConfigId;
        public float CurrentHealth;
        public float MaxHealth;
        [GhostField(Quantization = 100)] public float MaxSpeed;
        [GhostField(Quantization = 100)] public float Acceleration;
        public float DragCoefficient;
        public byte IsEnabled;
    }
}