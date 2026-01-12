namespace Starfire.Entity.Modules.Rotation
{
    public interface IRotationModule : IEntityModule
    {
        float RotationSpeed { get; }
        void ProcessRotation(RotationInputData input, float deltaTime);
    }
}
