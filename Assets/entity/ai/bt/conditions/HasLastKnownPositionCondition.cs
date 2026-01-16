namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Condition that succeeds when a last known position is stored.
    /// </summary>
    public class HasLastKnownPositionCondition : BTLeafCondition
    {
        private readonly string _lastPositionKey;

        public HasLastKnownPositionCondition(string lastPositionKey = "last_known_position")
        {
            _lastPositionKey = lastPositionKey;
        }

        protected override bool CheckCondition()
        {
            return Context.Has(_lastPositionKey);
        }
    }
}
