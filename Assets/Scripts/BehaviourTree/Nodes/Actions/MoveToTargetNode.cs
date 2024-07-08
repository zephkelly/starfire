using UnityEngine;

namespace Starfire
{
    public class MoveToTargetNode<T> : Node
    {
        private Ship ship;
        private StandardAICore aiCore;
        private T target;

        private float currentDistance;

        private Vector2 movementLerpVector;
        private Vector2 visualLerpVector;

        private LayerMask raycastAvoidanceLayers;
        private LayerMask raycastTargetLayers;

        public MoveToTargetNode(Ship _ship, T _target)
        {
            ship = _ship;
            aiCore = (StandardAICore)ship.Controller.AICore;
            target = _target;

            raycastTargetLayers = GetRaycastTargetLayers();
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
                    return Vector2.zero;
            }
        }

        public override NodeState Evaluate()
        {
            if (ship == null || target == null)
            {
                state = NodeState.Failure;
                return state;
            }

            Update();

            currentDistance = Vector2.Distance(ship.Controller.ShipTransform.position, GetTargetPosition());

            if (currentDistance < 10f)
            {
                state = NodeState.Success;
                return state;
            }
            else
            {
                state = NodeState.Running;
                return state;
            }
        }

        public void Update()
        {
            Vector2 weightedDirection = aiCore.FindBestDirection(
                ship.Controller.ShipObject,
                ship.Controller.ShipTransform.position,
                ship.Controller.ShipRigidBody.velocity.magnitude,
                GetTargetPosition(),
                raycastAvoidanceLayers,
                16,
                30f
            );

            movementLerpVector = weightedDirection;
            visualLerpVector = Vector2.Lerp(ship.Controller.ShipTransform.up, weightedDirection, 0.15f);
        }

        public override void FixedUpdate()
        {
            float speed = ship.Configuration.ThrusterMaxSpeed;
            ship.Controller.MoveInDirection(movementLerpVector, speed, true);
            ship.Controller.RotateToDirection(visualLerpVector, ship.Configuration.TurnDegreesPerSecond);
        }
    }
}