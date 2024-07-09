namespace Starfire
{
    public class CheckIfInStarOrbit : DecoratorNode
    {
        private Ship ship;

        public CheckIfInStarOrbit(Ship _ship, Node _child, Blackboard _blackboard) : base(_child, _blackboard)
        {
            ship = _ship;
        }

        public override NodeState Evaluate()
        {
            if (IsShipInOrbit())
            {
                return child.Evaluate();
            }
            else
            {
                return NodeState.Failure;
            }
        }

        public override void FixedEvaluate()
        {
            child.FixedEvaluate();
        }

        private bool IsShipInOrbit()
        {
            return ship.Controller.IsOrbiting;
        }
    }
}