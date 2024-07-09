using UnityEditor.Experimental.GraphView;

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

        public Node(Blackboard _blackboard)
        {
            blackboard = _blackboard;
        }

        protected NodeState state;
        protected Blackboard blackboard;
        public NodeState CurrentNodeState => state;

        public abstract NodeState Evaluate();
        public abstract void FixedEvaluate();
    }
}