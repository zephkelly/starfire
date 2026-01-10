namespace Starfire.Entity.Modules.AICore
{
    public interface IAICoreModule : IEntityModule
    {
        float ProcessingPower { get; }
        bool IsAutonomous { get; set; }
    }
}
