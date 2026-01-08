namespace Starfire.Entity.Modules.Rotation
{
    public interface IRotationModule : IShipModule
    {
        float RotationSpeed { get; }
        void ProcessRotation(RotationInputData input, float deltaTime);
    }
}
