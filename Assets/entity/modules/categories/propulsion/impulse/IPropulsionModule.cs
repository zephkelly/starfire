namespace Starfire.Entity.Modules.Propulsion
{
    public interface IPropulsionModule : IEntityModule
    {
        float MaxSpeed { get; }
        float Acceleration { get; }
        float Drag { get; }
    }
}
