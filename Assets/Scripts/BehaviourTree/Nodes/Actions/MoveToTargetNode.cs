using UnityEngine;

namespace Starfire
{
    public class MoveToTargetNode : Node
    {
        private Ship ship;
        private IAICore aiCore;
        private object target;

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

        public void SetTarget<T>(T _target)
        {
            target = _target;
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

        private Vector2 GetTargetPosition()
        {
            switch (target)
            {
                case Ship _ship:
                    return _ship.Controller.ShipTransform.position;
                case Vector2 _vector2:
                    return _vector2;
                case Transform _transform:
                    return _transform.position;
                default:
                    Debug.LogWarning("MoveToTargetNode: Target is not a valid type.");
                    return Vector2.zero;
            }
        }

        protected override NodeState OnEvaluate()
        {
            if (ship == null || target == null)
            {
                state = NodeState.Failure;
                return state;
            }

            float currentDistance = Vector2.Distance(ship.Controller.ShipTransform.position, GetTargetPosition());

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
                GetTargetPosition(),
                raycastTargetLayers,
                16,
                30f
            );

            movementLerpVector = weightedDirection;
            visualLerpVector = Vector2.Lerp(ship.Controller.ShipTransform.up, weightedDirection, 0.15f);

            float speed = ship.Configuration.ThrusterMaxSpeed;
            ship.Controller.MoveInDirection(movementLerpVector, speed, true);
            ship.Controller.RotateToDirection(visualLerpVector, ship.Configuration.TurnDegreesPerSecond);
        }
    }
}