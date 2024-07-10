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

        protected virtual void Initialise() { }

        protected virtual void Terminate() { }
        public NodeState Evaluate()
        {
            Initialise();

            state = OnEvaluate();

            Terminate();
            
            return state;
        }

        protected abstract NodeState OnEvaluate();
    }
}