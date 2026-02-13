namespace Starfire.Core
{
    public interface IAIController : IController
    {
        byte CurrentAIState { get; }
        int TargetEntityId { get; }
    }
}
