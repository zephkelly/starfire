namespace Starfire
{
    public class CheckIfInStarOrbit : Node
    {
        private Ship ship;

        public CheckIfInStarOrbit(Ship ship)
        {
            this.ship = ship;
        }

        protected override NodeState OnEvaluate()
        {
            if (IsShipInOrbit())
            {
                return NodeState.Success;
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