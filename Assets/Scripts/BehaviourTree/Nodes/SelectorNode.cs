using System.Collections.Generic;

namespace Starfire
{
    public class SelectorNode : Node
    {
        protected List<Node> nodes = new List<Node>();
        private Node activeNode;

        public SelectorNode() { }

        public void AddNode(Node node)
        {
            nodes.Add(node);
        }

        public override void Initialise()
        {
            activeNode = null;
        }

        public override NodeState Evaluate()
        {
            Node previousActiveNode = activeNode;

            foreach (Node node in nodes)
            {
                if (node != activeNode)
                {
                    node.Initialise();
                }

                NodeState result = node.Evaluate();
                switch (result)
                {
                    case NodeState.Success:
                    case NodeState.Running:
                        if (previousActiveNode != null && previousActiveNode != node)
                        {
                            previousActiveNode.Terminate();
                        }
                        activeNode = node;
                        state = result;
                        return state;
                    case NodeState.Failure:
                        if (node == activeNode)
                        {
                            node.Terminate();
                        }
                        continue;
                }
            }

            if (previousActiveNode != null)
            {
                previousActiveNode.Terminate();
            }

            activeNode = null;
            state = NodeState.Failure;
            return state;
        }

        public override void FixedEvaluate()
        {
            if (activeNode != null)
            {
                activeNode.FixedEvaluate();
            }
        }

        public override void Terminate()
        {
            if (activeNode != null)
            {
                activeNode.Terminate();
            }
            activeNode = null;
        }
    }
}