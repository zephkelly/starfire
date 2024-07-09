using UnityEditor.Experimental.GraphView;

namespace Starfire
{
    public abstract class DecoratorNode : Node
    {
        protected Node node;

        protected DecoratorNode(Node node)
        {
            this.node = node;
        }
    }
}