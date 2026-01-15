namespace Starfire.Entity.Modules.Propulsion
{
    public interface IPropulsionShipModule : IShipModule
    {
        float MaxSpeed { get; }
        float Acceleration { get; }
        float Drag { get; }
    }
}
