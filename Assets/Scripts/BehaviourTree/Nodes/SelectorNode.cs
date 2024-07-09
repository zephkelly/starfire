using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace Starfire
{
    public class SelectorNode : Node
    {
        protected List<Node> nodes = new List<Node>();

        public SelectorNode(Blackboard _blackboard) : base(_blackboard) { }

        public void AddNode(Node node)
        {
            nodes.Add(node);
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

        public override void FixedEvaluate()
        {
            foreach (Node node in nodes)
            {
                node.FixedEvaluate();
            }
        }
    }
}