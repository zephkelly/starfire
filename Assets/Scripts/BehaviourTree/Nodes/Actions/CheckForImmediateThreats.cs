using UnityEngine;

namespace Starfire
{
    public class CheckForImmediateThreats : Node
    {
        private Ship ship;

        public CheckForImmediateThreats(Ship ship)
        {
            this.ship = ship;
        }

        protected override NodeState OnEvaluate()
        {
            if (ship.AICore.Blackboard.ImmediateThreats.Count > 0)
            {
                state = NodeState.Success;
                return state;
            }

            state = NodeState.Failure;
            return NodeState.Failure;
        }
    }
}