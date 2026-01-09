namespace Starfire.Entity.Modules.Propulsion
{
    public interface IPropulsionModule : IShipModule
    {
        float MaxSpeed { get; }
        float Acceleration { get; }
        float Drag { get; }
    }
}
