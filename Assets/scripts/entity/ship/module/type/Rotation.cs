using Unity.Entities;

namespace Starfire.Entity
{
    public struct ShipRotation : IComponentData
    {
        public int ConfigId;
        public float CurrentHealth;
        public float MaxHealth;
        public float TurnRate;
        public float TargetHeading;
        public float CurrentHeading;
        public float AngularVelocity;
        public byte Mode;                  // RotationMode
        public byte State;                 // RotationState
        public byte RotationType;          // RotationType
        public byte IsEnabled;
    }
}