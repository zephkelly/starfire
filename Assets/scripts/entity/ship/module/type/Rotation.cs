using Unity.Entities;
using Unity.NetCode;

namespace Starfire.Entity
{
    public struct ShipRotation : IComponentData
    {
        [GhostField] public int ConfigId;
        public float CurrentHealth;
        public float MaxHealth;
        [GhostField(Quantization = 100)] public float TurnRate;
        [GhostField(Quantization = 1000)] public float TargetHeading;
        [GhostField(Quantization = 1000)] public float CurrentHeading;
        [GhostField(Quantization = 1000)] public float AngularVelocity;
        public byte Mode;
        public byte State;
        public byte RotationType;
        public byte IsEnabled;
    }
}