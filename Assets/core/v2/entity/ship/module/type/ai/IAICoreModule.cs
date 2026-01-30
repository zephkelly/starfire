namespace StarfireV2
{
    /// <summary>
    /// Interface for AI Core modules that provide autonomous behavior to entities.
    /// </summary>
    public interface IAICoreModule : IShipModule
    {
        float ProcessingPower { get; }
        bool IsAutonomous { get; set; }

        AIEntityControllerDriver Driver { get; }
        IBTNode BehaviorTree { get; }
        BTContext Context { get; }

        void SetBehaviorTree(IBTNode tree);
        void ApplyGoalParameters(GoalParameters parameters, bool clearPrevious = true);
    }
}
