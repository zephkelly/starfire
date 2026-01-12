namespace Starfire.Entity.Modules.Sensor
{
    public interface ISensorModule : IEntityModule
    {
        float DetectionRange { get; }
        float TargetingAccuracy { get; }
    }
}
