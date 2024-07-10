namespace Starfire
{
    public class CheckIfInStarOrbit : DecoratorNode
    {
        private Ship ship;

        public CheckIfInStarOrbit(Ship ship, Node node) : base(node)
        {
            this.ship = ship;
        }

        protected override NodeState OnEvaluate()
        {
            if (IsShipInOrbit())
            {
                return node.Evaluate();
            }
            else
            {
                return NodeState.Failure;
            }
        }

        private bool IsShipInOrbit()
        {
            return ship.Controller.IsOrbiting;
        }
    }
}