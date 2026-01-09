namespace Starfire.Entity.Modules.Sensor
{
    public interface ISensorModule : IShipModule
    {
        float DetectionRange { get; }
        float TargetingAccuracy { get; }
    }
}
