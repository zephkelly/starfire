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

        public MoveToTargetNode(Ship _ship, Blackboard _blackboard) : base(_blackboard)
        {
            ship = _ship;
            aiCore = _ship.AICore;

            raycastTargetLayers = GetRaycastTargetLayers();
        }

        public void SetTarget<T>(T _target)
        {
            target = _target;
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

        public override NodeState Evaluate()
        {
            if (ship == null || target == null)
            {
                state = NodeState.Failure;
                return state;
            }

            Update();

            float currentDistance = Vector2.Distance(ship.Controller.ShipTransform.position, GetTargetPosition());

            if (currentDistance < targetReachedDistance)
            {
                state = NodeState.Success;
                ship.Controller.DisableThrusters();

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
            Vector2 weightedDirection = aiCore.CalculateAvoidanceSteeringDirection(
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

        public override void FixedEvaluate()
        {
            if (ship == null || target == null || state == NodeState.Success || state == NodeState.Failure)
            {

                return;
            }

            float speed = ship.Configuration.ThrusterMaxSpeed;
            ship.Controller.MoveInDirection(movementLerpVector, speed, true);
            ship.Controller.RotateToDirection(visualLerpVector, ship.Configuration.TurnDegreesPerSecond);
        }
    }
}