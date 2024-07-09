namespace Starfire
{
    public abstract class Node
    {
        public enum NodeState
        {
            Running,
            Success,
            Failure
        }

        protected NodeState state;
        public NodeState CurrentNodeState => state;

        public abstract void Initialise();
        public abstract NodeState Evaluate();
        public abstract void FixedEvaluate();
        public abstract void Terminate();
    }
}