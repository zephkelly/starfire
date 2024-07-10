using UnityEngine;

namespace Starfire
{
    public class MoveToHeading : Node
    {
        private Ship ship;


        private int numberOfRays = 16;
        private float collisionCheckRadius = 20f;

        private LayerMask whichRaycastableLayers = LayerMask.GetMask("Player", "Friend");

        public MoveToHeading(Ship _ship)
        {
            ship = _ship;
        }

        protected override NodeState OnEvaluate()
        {
            if (ship == null || ship.AICore.Blackboard.CurrentTarget == null)
            {
                state = NodeState.Failure;
                return state;
            }

            Vector2 currentHeading = ship.AICore.Blackboard.CurrentHeading;

            currentHeading = ship.AICore.CalculateAvoidanceSteeringDirection(
                ship.Controller.ShipObject,
                ship.Controller.ShipTransform.position,
                ship.Controller.ShipRigidBody.velocity.magnitude,
                currentHeading,
                whichRaycastableLayers,
                numberOfRays,
                collisionCheckRadius
            );

            Vector2 shipPosition = ship.Controller.ShipTransform.position;
            Vector2 targetPosition = ship.AICore.Blackboard.GetCurrentTargetPosition();

            float thrusterMaxSpeed = ship.Configuration.ThrusterMaxSpeed;
            ship.Controller.MoveInDirection(ship.AICore.Blackboard.CurrentHeading, thrusterMaxSpeed, true);
            
            state = NodeState.Running;
            return state;
        }
    }
}