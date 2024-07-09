using UnityEditor.Experimental.GraphView;

namespace Starfire
{
    public abstract class DecoratorNode : Node
    {
        protected Node child;

        protected DecoratorNode(Node child, Blackboard _blackboard) : base(_blackboard)
        {
            this.child = child;
        }

        public override void FixedEvaluate()
        {
            child.FixedEvaluate();
        }
    }
}