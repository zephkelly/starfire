using Starfire.Entity.AI.BT;

namespace Starfire.Entity.Modules.AICore
{
    public interface IAICoreModule : IEntityModule
    {
        float ProcessingPower { get; }
        bool IsAutonomous { get; set; }

        AIDriver Driver { get; }
        IBTNode BehaviorTree { get; }
        BTContext Context { get; }

        void SetBehaviorTree(IBTNode tree);
    }
}
