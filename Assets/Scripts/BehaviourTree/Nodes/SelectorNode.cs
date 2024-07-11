using System.Collections.Generic;

namespace Starfire
{
    public class SelectorNode : Node
    {
        protected List<Node> nodes = new List<Node>();
        
        public void AddNode(Node node)
        {
            nodes.Add(node);
        }

        protected override NodeState OnEvaluate()
        {
            foreach (Node node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Failure:
                        continue;
                    case NodeState.Success:
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                    default:
                        continue;
                }
            }

            return NodeState.Failure;
        }

        protected override void OnFixedEvaluate()
        {
            foreach (Node node in nodes)
            {
                if (node.CurrentNodeState == NodeState.Running)
                {
                    node.FixedEvaluate();
                }
            }
        }
    }
}