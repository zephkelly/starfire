using UnityEngine;
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

        protected override NodeState OnEvaluate()
        {
            bool anyNodeRunning = false;
            foreach (Node node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Failure:
                        return NodeState.Failure;
                    case NodeState.Success:
                        continue;
                    case NodeState.Running:
                        anyNodeRunning = true;
                        break;
                    default:
                        continue;
                }
            }
            return anyNodeRunning ? NodeState.Running : NodeState.Success;
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