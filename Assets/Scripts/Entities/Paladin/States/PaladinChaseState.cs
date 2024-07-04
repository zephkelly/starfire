using UnityEngine;

namespace Starfire
{
    public class PaladinChaseState : IState
    {
        private PaladinShipController _shipController;
        private StandardAICore _shipCore;
        private Target _currentTarget;

        private StateMachine _stateMachine;
        private GameObject _paladinObject;
        private Rigidbody2D _paladinRigid2D;
        private Transform _paladinTransform;

        private Vector2 lerpVector;
        private Vector2 visualLerpVector;

        private LayerMask whichRaycastableAvoidanceLayers;
        private LayerMask whichRaycastableTargetLayers;
        private int numberOfRays = 16;
        private float chaseRadius = 300f;
        private float collisionCheckRadius = 30f;
        private float targetSightDistance = 200f;
        private float targetSightAngle = 90f;

        private float timeToSpendCirclingTillStateChange;

        public PaladinChaseState(PaladinShipController paladinController)
        {
            _shipController = paladinController;
            _shipCore = (StandardAICore)_shipController.ShipCore;
            _currentTarget = _shipController.ShipCore.CurrentTarget;

            _stateMachine = _shipController.StateMachine;
            _paladinObject = _shipController.ShipObject;
            _paladinRigid2D = _shipController.ShipRigidBody;
            _paladinTransform = _shipController.ShipTransform;
        }

        public void Enter()
        {
            whichRaycastableTargetLayers = LayerMask.GetMask("Enemy");
            timeToSpendCirclingTillStateChange = UnityEngine.Random.Range(3f, 6f);
        }

        public void Execute()
        {
            if (_currentTarget == null)
            {
                _stateMachine.ChangeState(new PaladinIdleState(_shipController));
                return;
            }

            if (_shipCore.TimeSpentCircling > timeToSpendCirclingTillStateChange)
            {
                _stateMachine.ChangeState(new PaladinCircleState(_shipController));
                return;
            }

            if (_currentTarget == null) 
            {
                _shipController.StateMachine.ChangeState(new PaladinIdleState(_shipController));
                return;
            }

            Vector2 lastKnownTargetPosition = _shipCore.GetTargetPosition(
                _paladinObject,
                _paladinTransform.position,
                _paladinRigid2D.velocity,
                _currentTarget.GetPosition(), 
                chaseRadius,
                whichRaycastableTargetLayers
            );

            Vector2 weightedDirection = _shipCore.FindBestDirection(
                _paladinObject,
                _paladinTransform.position, 
                _currentTarget.GetPosition(),
                _paladinRigid2D.velocity.magnitude,
                numberOfRays,
                collisionCheckRadius,
                whichRaycastableAvoidanceLayers
            );

            weightedDirection = _shipCore.CircleTarget(weightedDirection, _paladinTransform.position, _paladinRigid2D.velocity, lastKnownTargetPosition);
            bool isPlayerInSight = _shipCore.IsTargetWithinSight(_paladinTransform.position, _paladinTransform.up,  lastKnownTargetPosition, targetSightDistance, targetSightAngle);

            lerpVector = Vector2.Lerp(_paladinTransform.up, weightedDirection, 0.7f).normalized;
            visualLerpVector = Vector2.Lerp(_paladinTransform.up, weightedDirection, 0.15f);

            if (_shipCore.CanFireProjectile() && isPlayerInSight)
            {
                Vector2 firingPosition = _shipCore.GetProjectileFiringPosition(
                    _paladinTransform.position,
                    lastKnownTargetPosition
                );
                _shipController.FireProjectileToPosition(firingPosition);
            }
        }

        public void FixedUpdate()
        {
            float distance = 0f;

            if (_currentTarget != null)
            {
                distance = Vector2.Distance(_paladinTransform.position, _currentTarget.GetPosition());
            }

            float speedMultiplier = GetSpeedMultiplier(distance);
            float speed = _shipController.Ship.Configuration.ThrusterMaxSpeed * speedMultiplier;

            _shipController.MoveInDirection(lerpVector, speed, true);
            _shipController.RotateToDirection(visualLerpVector, _shipController.Ship.Configuration.TurnDegreesPerSecond);
        }

        private float GetSpeedMultiplier(float distance)
        {
            float minSpeedMultiplier = 0.5f;
            float maxSpeedMultiplier = 1.1f;
            return Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, Mathf.InverseLerp(0, 220, distance));
        }

        public void Exit() { }
    }
}