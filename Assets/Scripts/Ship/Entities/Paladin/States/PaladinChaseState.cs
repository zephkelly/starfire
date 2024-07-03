using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starfire
{
    public class PaladinChaseState : IState
    {
        private PaladinShipController _shipController;
        private StateMachine _stateMachine;
        private GameObject _paladinObject;
        private Rigidbody2D _paladinRigid2D;
        private Transform _paladinTransform;
        private Transform _targetTransform;
        private Rigidbody2D _targetRigid2D;

        // Raycasting variables
        private Vector2 lerpVector;
        private Vector2 visualLerpVector;
        private LayerMask whichRaycastableLayers;
        private int numberOfRays = 16;
        private float chaseRadius = 300f;
        private float collisionCheckRadius = 30f;
        private float targetSightDistance = 200f;
        private float targetSightAngle = 90f;

        private float timeToSpendCirclingTillStateChange;

        public PaladinChaseState(PaladinShipController _paladin)
        {
            _shipController = _paladin;
            _stateMachine = _paladin.ScavengerStateMachine;
            _paladinObject = _paladin.ScavengerObject;
            _paladinRigid2D = _paladin.ScavengerRigidbody;
            _paladinTransform = _paladin.ScavengerTransform;
            _targetTransform = _paladin.TargetTransform;
            _targetRigid2D = _paladin.TargetRigidbody;
        }

        public void Enter()
        {
            whichRaycastableLayers = LayerMask.GetMask("Enemy", "Player");
            timeToSpendCirclingTillStateChange = UnityEngine.Random.Range(3f, 6f);
        }

        public void Execute()
        {
            if (_targetTransform == null)
            {
                _stateMachine.ChangeState(new PaladinIdleState(_shipController));
                return;
            }

            if (_shipController.TimeSpentCircling > timeToSpendCirclingTillStateChange)
            {
                _stateMachine.ChangeState(new PaladinCircleState(_shipController, _paladinRigid2D, _targetTransform));
                return;
            }

            Vector2 lastKnownTargetPosition = _shipController.GetTargetPosition(
                _paladinObject,
                _paladinTransform.position,
                _paladinRigid2D.velocity,
                _targetTransform.position,
                chaseRadius,
                whichRaycastableLayers
            );

            Vector2 weightedDirection = _shipController.FindBestDirection(
                _paladinObject,
                _paladinTransform.position, 
                _targetTransform.position,
                _paladinRigid2D.velocity.magnitude,
                numberOfRays,
                collisionCheckRadius,
                whichRaycastableLayers
            );

            weightedDirection = _shipController.CirclePlayer(weightedDirection, _paladinTransform.position, _paladinRigid2D.velocity, lastKnownTargetPosition);

            lerpVector = Vector2.Lerp(_paladinTransform.up, weightedDirection, 0.7f);
            visualLerpVector = Vector2.Lerp(_paladinTransform.up, weightedDirection, 0.15f);

            bool isPlayerInSight = _shipController.IsPlayerWithinSight(_paladinTransform.position, lastKnownTargetPosition, targetSightDistance, targetSightAngle);

            if (_shipController.CanFireProjectile() && isPlayerInSight)
            {
                Vector2 firingPosition = _shipController.GetProjectileFiringPosition(_paladinTransform.position, lastKnownTargetPosition);
                _shipController.FireProjectileToPosition(firingPosition);
            }
        }

        public void FixedUpdate()
        {
            float distance = 0f;

            if (_targetTransform != null)
            {
                distance = Vector2.Distance(_paladinTransform.position, _targetTransform.position);
            }

            float speedMultiplier = GetSpeedMultiplier(distance);
            float speed = _shipController.Configuration.ThrusterMaxSpeed * speedMultiplier;

            _shipController.MoveInDirection(lerpVector, speed, true);
            _shipController.RotateToDirection(visualLerpVector, _shipController.Configuration.TurnDegreesPerSecond);
        }

        private float GetSpeedMultiplier(float distance)
        {
            float minSpeedMultiplier = 0.5f;
            float maxSpeedMultiplier = 1.1f;
            return Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, Mathf.InverseLerp(0, 220, distance));
        }

        public void Exit()
        {

        }
    }
}
