using Starfire.Entity.Modules.Rotation;

namespace StarfireV2
{
    public interface IShipRotationModule : IShipModule
    {
        float RotationSpeed { get; }
        void ProcessRotation(RotationInputData input, float deltaTime);
    }
}
