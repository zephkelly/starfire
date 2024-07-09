using System.Collections.Generic;

namespace Starfire
{
    public class SequenceNode : Node
    {
        protected List<Node> nodes = new List<Node>();

        public void AddNode(Node node)
        {
            nodes.Add(node);
        }

        public override void Initialise()
        {
            foreach (Node node in nodes)
            {
                node.Initialise();
            }
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

        public override void FixedEvaluate()
        {
            foreach (Node node in nodes)
            {
                if (node.CurrentNodeState != NodeState.Running) return;
                node.FixedEvaluate();
            }
        }

        public override void Terminate()
        {
            foreach (Node node in nodes)
            {
                node.Terminate();
            }
        }
    }
}