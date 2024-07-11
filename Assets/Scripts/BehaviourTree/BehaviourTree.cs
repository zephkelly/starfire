using UnityEngine;

namespace Starfire
{
    public class BehaviourTree
    {
        private Node rootNode;

        public BehaviourTree(Node _rootNode)
        {
            rootNode = _rootNode;
        }

        public void Evaluate()
        {
            if (rootNode != null)
            {
                rootNode.Evaluate();
            }
        }

        public void FixedEvaluate()
        {
            if (rootNode != null)
            {
                rootNode.FixedEvaluate();
            }
        }
    }
}