using System.Collections.Generic;

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

        public abstract NodeState Evaluate();
    }
}