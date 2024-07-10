using UnityEngine;

namespace Starfire
{
    public class AddCircleBiasToHeading : Node
    {
        private Ship ship;

        private float orbitRadius = 150f;
        private float currentOrbitRadius = 100f;
        private int orbitDirection = 1;
        private float orbitTimer = 0f;

        public AddCircleBiasToHeading(Ship ship)
        {
            this.ship = ship;
        }

        protected override NodeState OnEvaluate()
        {
            Vector2 currentTargetPosition = ship.AICore.Blackboard.GetCurrentTargetPosition();

            if (currentTargetPosition == null)
            {
                state = NodeState.Failure;
                return state;
            }

            if (orbitTimer <= 0)
            {
                orbitTimer = Random.Range(8f, 12f);
                orbitDirection = orbitDirection == 1 ? -1 : 1;

                currentOrbitRadius = Random.Range(orbitRadius * 0.7f, orbitRadius * 1.3f);
            }
            else
            {
                orbitTimer -= Time.deltaTime;
            }

            Vector2 currentHeading = ship.AICore.Blackboard.CurrentHeading;

            Vector2 newBiasedHeading = ship.AICore.AddCircleTargetBias(
                currentHeading,
                ship.Controller.ShipTransform.position,
                ship.Controller.ShipRigidBody.velocity,
                currentTargetPosition,
                currentOrbitRadius,
                orbitDirection
            );

            newBiasedHeading = Vector2.Lerp(ship.Controller.ShipTransform.up, newBiasedHeading, 0.7f);

            ship.AICore.Blackboard.SetCurrentHeadingAndNormalise(newBiasedHeading);   
            state = NodeState.Running;
            return state;
        }
    }
}