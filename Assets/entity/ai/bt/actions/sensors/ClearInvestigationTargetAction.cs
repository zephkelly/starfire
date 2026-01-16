namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Clears the current investigation target from blackboard.
    /// Always returns Success.
    /// </summary>
    public class ClearInvestigationTargetAction : BTAction
    {
        private readonly string _targetKey;

        public ClearInvestigationTargetAction(string targetKey = "investigation_target")
        {
            _targetKey = targetKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            Context.Remove(_targetKey);
            return BTNodeStatus.Success;
        }
    }
}
