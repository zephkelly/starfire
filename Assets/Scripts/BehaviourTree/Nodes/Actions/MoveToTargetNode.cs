using UnityEngine;

namespace Starfire
{
    public class MoveToTargetNode : Node
    {
        private Ship ship;
        private IAICore aiCore;
        // 

        private Vector2 movementLerpVector;
        private Vector2 visualLerpVector;

        private LayerMask raycastAvoidanceLayers;
        private LayerMask raycastTargetLayers;

        private float targetReachedDistance = 40f;

        public MoveToTargetNode(Ship _ship)
        {
            ship = _ship;
            aiCore = _ship.AICore;

            raycastAvoidanceLayers = GetRaycastTargetLayers();
        }

        protected override void Initialise()
        {
            Debug.Log("MoveToTargetNode: Initialise");
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

            float currentDistance = Vector2.Distance(shipPosition, targetPosition);

            if (currentDistance < targetReachedDistance)
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
                16,
                30f
            );

            movementLerpVector = weightedDirection.normalized;
            visualLerpVector = Vector2.Lerp(ship.Controller.ShipTransform.up, weightedDirection, 0.15f).normalized;

            float speed = ship.Configuration.ThrusterMaxSpeed;
            ship.Controller.MoveInDirection(movementLerpVector, speed, true);
            ship.Controller.RotateToDirection(visualLerpVector, ship.Configuration.TurnDegreesPerSecond);
        }
    }
}