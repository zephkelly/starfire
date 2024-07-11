using UnityEngine;

namespace Starfire
{
    public class AddCircleTargetBias : Node
    {
        private Ship ship;

        private float orbitRadius;
        private float currentOrbitRadius;
        private int orbitDirection;
        private int lastOrbitDirection;

        private float orbitTime;
        private float orbitTimer = 0f;


        public AddCircleTargetBias(Ship ship, float orbitRadius = 150f, float orbitTime = 8f)
        {
            this.ship = ship;
            this.orbitTime = orbitTime;
            this.orbitRadius = orbitRadius;
            currentOrbitRadius = orbitRadius;
        }

        protected override NodeState OnEvaluate()
        {

            Vector2 currentTargetPosition = ship.AICore.Blackboard.GetCurrentTargetPosition();
            float distanceToTargetPosition = Vector2.Distance(ship.Controller.ShipTransform.position, currentTargetPosition);

            if (currentTargetPosition == null)
            {
                state = NodeState.Failure;
                return state;
            }

            if (distanceToTargetPosition > currentOrbitRadius)
            {
                state = NodeState.Failure;
                return state;
            }

            UpdateOrbitingTimers();

            Vector2 currentHeading = ship.AICore.Blackboard.CurrentHeading;
            Vector2 newHeading = AddCircleBias(currentHeading, currentTargetPosition, distanceToTargetPosition);

            ship.AICore.Blackboard.SetCurrentHeadingAndNormalise(newHeading);
            state = NodeState.Running;
            return state;
        }

        private void UpdateOrbitingTimers()
        {
            if (orbitTimer > 0)
            {
                orbitTimer -= Time.deltaTime;
                return;
            }

            orbitDirection = orbitDirection == 1 ? -1 : 1;
            currentOrbitRadius = Random.Range(orbitRadius * 0.8f, orbitRadius * 1.4f);
            orbitTimer = Random.Range(orbitTime * 0.6f, orbitTime * 1.4f);
        }

        private Vector2 AddCircleBias(Vector2 currentHeading, Vector2 targetPosition, float distanceToTarget)
        {
            Vector2 targetDirection = new Vector2(
                targetPosition.x - ship.Controller.ShipTransform.position.x,
                targetPosition.y - ship.Controller.ShipTransform.position.y
            ).normalized;

            Vector2 tangentToTargetDirection = Vector2.Perpendicular(targetDirection) * lastOrbitDirection;

            if (orbitDirection != lastOrbitDirection)
            {
                lastOrbitDirection = orbitDirection;
            }

            Vector2 biasDirection = tangentToTargetDirection;

            if (orbitTimer > 0)
            {
                float outwardBiasStrength = Mathf.Clamp01(orbitTimer / orbitTime);
                Vector2 outwardBias = -targetDirection * outwardBiasStrength;
                biasDirection = (tangentToTargetDirection + outwardBias).normalized;
            }

            float biasMagnitude = Mathf.InverseLerp(currentOrbitRadius, 0, distanceToTarget);
            float playerVelocityMagnitude = ship.Controller.ShipRigidBody.velocity.magnitude;
            float biasMultiplier = playerVelocityMagnitude > 50f ? 3f : 5f;

            Vector2 newDirection = currentHeading + biasDirection * (biasMagnitude * biasMultiplier);
            newDirection.Normalize();

            return newDirection;
        }
    }
}