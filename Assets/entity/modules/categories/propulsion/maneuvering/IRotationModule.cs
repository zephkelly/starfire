namespace Starfire.Entity.Modules.Rotation
{
    public interface IRotationShipModule : IShipModule
    {
        float RotationSpeed { get; }
        void ProcessRotation(RotationInputData input, float deltaTime);
    }
}
