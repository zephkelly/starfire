using UnityEngine;

namespace Starfire
{
    public class SimpleMoveToTarget : Node
    {
        private Ship ship;
        private IAICore aiCore;
        // 

        private Vector2 movementLerpVector;
        private Vector2 visualLerpVector;

        // private LayerMask raycastAvoidanceLayers;
        private LayerMask raycastTargetLayers;
        private int numberOfRays = 16;
        private float collisionCheckRadius = 30f;

        private float minSpeedMultiplier = 0.4f;
        private float maxSpeedMultiplier = 1.15f;
        private float speedMultiplierRange = 400f;
        private float targetReachedThreshold = 40f;
        private float currentTargetDistance = 0;

        public SimpleMoveToTarget(Ship _ship, float targetReachedThreshold)
        {
            ship = _ship;
            aiCore = _ship.AICore;
            this.targetReachedThreshold = targetReachedThreshold;
        }

        private LayerMask GetRaycastTargetLayers()
        {
            switch(ship.Transponder.Faction)
            {
                case Faction.Enemy:
                    return LayerMask.GetMask("Friend", "Player");
                default:
                    return LayerMask.GetMask("Enemy", "Friend", "Player");
            }
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

            currentTargetDistance = Vector2.Distance(shipPosition, targetPosition);

            if (currentTargetDistance < targetReachedThreshold)
            {
                state = NodeState.Success;
                return state;
            }

            SteerShipToTarget();
            
            state = NodeState.Running;
            return state;
        }

        private void SteerShipToTarget()
        {
            Vector2 weightedDirection = aiCore.CalculateAvoidanceSteeringDirection(
                ship.Controller.ShipObject,
                ship.Controller.ShipTransform.position,
                ship.Controller.ShipRigidBody.velocity.magnitude,
                ship.AICore.Blackboard.GetCurrentTargetPosition(),
                raycastTargetLayers,
                numberOfRays,
                collisionCheckRadius
            );

            movementLerpVector = weightedDirection.normalized;
            visualLerpVector = Vector2.Lerp(ship.Controller.ShipTransform.up, weightedDirection, 0.15f).normalized;

            float thrusterMaxSpeed = ship.Configuration.ThrusterMaxSpeed;
            float speedMultiplier = GetSpeedMultiplier(currentTargetDistance);
            float speed = thrusterMaxSpeed * speedMultiplier;

            ship.Controller.MoveInDirection(movementLerpVector, speed, true);
            ship.Controller.RotateToDirection(visualLerpVector, ship.Configuration.TurnDegreesPerSecond);
        }

        private float GetSpeedMultiplier(float distance)
        {
            float lerpValue = Mathf.InverseLerp(0, speedMultiplierRange, distance);
            return Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, lerpValue);
        }
    }
}