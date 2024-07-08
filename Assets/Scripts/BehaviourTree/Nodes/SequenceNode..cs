using System.Collections.Generic;

namespace Starfire
{
    public class SequenceNode : Node
    {
        protected List<Node> nodes = new List<Node>();

        public SequenceNode(List<Node> nodes)
        {
            this.nodes = nodes;
        }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;

            foreach (Node node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Failure:
                        state = NodeState.Failure;
                        return state;
                    case NodeState.Running:
                        anyChildRunning = true;
                        continue;
                    case NodeState.Success:
                        continue;
                    default:
                        continue;
                }
            }

            state = anyChildRunning ? NodeState.Running : NodeState.Success;
            return state;
        }
    }
}