using System.Collections.Generic;

namespace Starfire
{
    public class SelectorNode : Node
    {
        protected List<Node> nodes = new List<Node>();

        public SelectorNode(List<Node> nodes)
        {
            this.nodes = nodes;
        }

        public override NodeState Evaluate()
        {
            foreach (Node node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Success:
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                    case NodeState.Failure:
                        continue;
                    default:
                        continue;
                }
            }

            state = NodeState.Failure;
            return state;
        }
    }
}