namespace StarfireV2
{
    public interface IShipPropulsionModule : IShipModule
    {
        float MaxSpeed { get; }
        float Acceleration { get; }
        float Drag { get; }
    }
}
