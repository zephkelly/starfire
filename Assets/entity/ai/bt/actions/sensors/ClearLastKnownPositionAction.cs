namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Clears the last known position after searching.
    /// Always returns Success.
    /// </summary>
    public class ClearLastKnownPositionAction : BTAction
    {
        private readonly string _lastPositionKey;

        public ClearLastKnownPositionAction(string lastPositionKey = "last_known_position")
        {
            _lastPositionKey = lastPositionKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            Context.Remove(_lastPositionKey);
            return BTNodeStatus.Success;
        }
    }
}
