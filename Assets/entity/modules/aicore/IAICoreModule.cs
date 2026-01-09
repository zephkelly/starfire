namespace Starfire.Entity.Modules.AICore
{
    public interface IAICoreModule : IShipModule
    {
        float ProcessingPower { get; }
        bool IsAutonomous { get; set; }
    }
}
