namespace Starfire
{
    public class CheckIfInStarOrbit : DecoratorNode
    {
        private Ship ship;

        public CheckIfInStarOrbit(Ship ship, Node node) : base(node)
        {
            this.ship = ship;
        }

        public override void Initialise() { }

        public override NodeState Evaluate()
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

        public override void FixedEvaluate() { }

        public override void Terminate() { }
    }
}