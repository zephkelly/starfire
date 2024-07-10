using UnityEngine;

namespace Starfire
{
    public class GetHeadingToTarget : Node
    {
        private Ship ship;

        public GetHeadingToTarget(Ship ship)
        {
            this.ship = ship;
        }

        protected override NodeState OnEvaluate()
        {
            if (ship.AICore.Blackboard.ImmediateThreats.Count < 0)
            {
                state = NodeState.Failure;
                return state;
            }

            Vector3 currentTargetPosition = ship.AICore.Blackboard.GetCurrentTargetPosition();

            if (currentTargetPosition == null)
            {
                state = NodeState.Failure;
                return state;
            }

            Vector2 directionToTarget = currentTargetPosition - ship.Controller.ShipTransform.position;
            ship.AICore.Blackboard.SetCurrentHeadingAndNormalise(directionToTarget);

            state = NodeState.Running;
            return state;
        }
    }
}