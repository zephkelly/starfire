using UnityEngine;

namespace Starfire
{
    public class SteerToTargetStern : Node
    {
        private Ship ship;

        private float minimumSideFollowDistance;
        private float minimumRearFollowDistance;

        private float sideChangeTimer;
        private float sideChangeInterval;
        private bool preferLeftSide = true;

        public SteerToTargetStern(Ship ship, float sideFollowDistance = 60f, float rearFollowDistance = 35f)
        {
            this.ship = ship;

            minimumSideFollowDistance = sideFollowDistance;
            minimumRearFollowDistance = rearFollowDistance;

            sideChangeInterval = Random.Range(10f, 20f);
            sideChangeTimer = sideChangeInterval;
            preferLeftSide = Random.value > 0.5f;
        }

        protected override NodeState OnEvaluate()
        {

            if (ship.AICore.Blackboard.CurrentTarget is not Ship)
            {
                state = NodeState.Failure;
                return state;
            }

            Ship currentTarget = ship.AICore.Blackboard.CurrentTarget as Ship;

            if (currentTarget == null)
            {
                state = NodeState.Failure;
                return state;
            }

            float headingDotProduct = DotProductOfHeadingDirection(currentTarget);

            if (headingDotProduct < 0.8f)
            {
                state = NodeState.Failure;
                return state;
            }

            Vector2 currentHeading = ship.AICore.Blackboard.CurrentHeading;
            Vector2 newHeading = SetHeadingToTargetStern(currentTarget, currentHeading, headingDotProduct);

            ship.AICore.Blackboard.SetCurrentHeadingAndNormalise(newHeading);
            state = NodeState.Running;
            return state;
        }

        private void UpdateSideChangeTimer()
        {
            if (sideChangeTimer > 0)
            {
                sideChangeTimer -= Time.deltaTime;
                return;
            }

            preferLeftSide = !preferLeftSide;
            sideChangeInterval = Random.Range(10f, 20f);
            sideChangeTimer = sideChangeInterval;
        }

        private float DotProductOfHeadingDirection(Ship currentTarget)
        {
            Vector2 ourVelocity = ship.Controller.ShipRigidBody.velocity;
            Vector2 targetVelocity = currentTarget.Controller.ShipRigidBody.velocity;
            
            if (ourVelocity.magnitude > 0.1f && targetVelocity.magnitude > 0.1f)
            {
                return Vector2.Dot(targetVelocity.normalized, ourVelocity.normalized);
            }
            
            return -1f;
        }

        private Vector2 SetHeadingToTargetStern(Ship currentTarget, Vector2 currentHeading, float headingDotProduct)
        {
            Vector2 targetPosition = currentTarget.Controller.ShipTransform.position;
            Vector2 targetVelocity = currentTarget.Controller.ShipRigidBody.velocity;
            Vector2 shipPosition = ship.Controller.ShipTransform.position;

            Vector2 directionToTarget = (targetPosition - shipPosition).normalized;
            Vector2 perpendicularToTarget = Vector2.Perpendicular(directionToTarget).normalized;

            float currentDistance = Vector2.Distance(shipPosition, targetPosition);
            float desiredDistance = minimumRearFollowDistance + minimumSideFollowDistance;

            Vector2 behindTargetPosition = targetPosition - targetVelocity.normalized * minimumRearFollowDistance;
            Vector2 leftFollowPosition = behindTargetPosition + perpendicularToTarget * minimumSideFollowDistance;
            Vector2 rightFollowPosition = behindTargetPosition - perpendicularToTarget * minimumSideFollowDistance;

            Vector2 distanceToLeftFollowPosition = leftFollowPosition - shipPosition;
            Vector2 distanceToRightFollowPosition = rightFollowPosition - shipPosition;

            // Vector2 finalChosenPosition = distanceToLeftFollowPosition.magnitude < distanceToRightFollowPosition.magnitude 
            //     ? leftFollowPosition 
            //     : rightFollowPosition;
            Vector2 finalChosenPosition = preferLeftSide ? leftFollowPosition : rightFollowPosition;

            Vector2 newHeading = (finalChosenPosition - shipPosition).normalized;

            bool isInFront = Vector2.Dot(directionToTarget, targetVelocity.normalized) < 0;
            
            if (currentDistance < desiredDistance || isInFront)
            {
                float distanceRatio = Mathf.Clamp01(currentDistance / desiredDistance);
                
                if (isInFront)
                {
                    Vector2 pointBehindTarget = targetPosition - targetVelocity.normalized * (minimumRearFollowDistance * 0.5f);
                    Vector2 directionToBehindPoint = (pointBehindTarget - shipPosition).normalized;
                    newHeading = Vector2.Lerp(directionToBehindPoint, newHeading, distanceRatio);
                }
                else
                {
                    Vector2 awayFromTarget = -directionToTarget;
                    newHeading = Vector2.Lerp(awayFromTarget, newHeading, distanceRatio);
                }
            }

            return newHeading;
        }
    }
}