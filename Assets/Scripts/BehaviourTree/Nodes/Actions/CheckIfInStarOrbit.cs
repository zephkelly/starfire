using UnityEngine;

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
            if (ship.Controller.IsOrbiting)
            {
                state = NodeState.Success;
                return state;
            }

            state = NodeState.Failure;
            return state;
        }
    }
}