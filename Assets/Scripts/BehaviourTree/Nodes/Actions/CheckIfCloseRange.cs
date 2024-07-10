using UnityEngine;

namespace Starfire
{
    public class CheckIfCloseRange : Node
    {
        private Ship ship;

        public CheckIfCloseRange(Ship ship)
        {
            this.ship = ship;
        }

        protected override NodeState OnEvaluate()
        {
            if (ship == null || ship.AICore.Blackboard.CurrentTarget == null)
            {
                state = NodeState.Failure;
                return state;
            }

            Vector2 shipPosition = ship.Controller.ShipTransform.position;
            Vector2 targetPosition = ship.AICore.Blackboard.GetCurrentTargetPosition();

            float currentTargetDistance = Vector2.Distance(shipPosition, targetPosition);

            if (currentTargetDistance < 150)
            {
                Debug.DrawLine(shipPosition, targetPosition, Color.green);
                state = NodeState.Success;
                return state;
            }

            state = NodeState.Failure;
            return state;
        }
    }
}