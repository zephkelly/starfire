using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starfire
{
    public class ScavengerChaseState : IState
    {
        private ScavengerShipController _shipController;
        private StandardAICore _shipCore;
        private Target _currentTarget;

        private StateMachine _stateMachine;
        private GameObject _scavengerObject;
        private Rigidbody2D _scavengerRigid2D;
        private Transform _scavengerTransform;

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

        public ScavengerChaseState(ScavengerShipController scavengerController)
        {
            _shipController = scavengerController;
            _shipCore = (StandardAICore)scavengerController.ShipCore;
            _currentTarget = scavengerController.ShipCore.CurrentTarget;

            _stateMachine = scavengerController.StateMachine;
            _scavengerObject = scavengerController.ShipObject;
            _scavengerRigid2D = scavengerController.ShipRigidBody;
            _scavengerTransform = scavengerController.ShipTransform;
        }

        public void Enter()
        {
            whichRaycastableTargetLayers = LayerMask.GetMask("Friend", "Player");
            timeToSpendCirclingTillStateChange = UnityEngine.Random.Range(3f, 6f);
        }

        public void Execute()
        {
            if (_currentTarget == null)
            {
                _stateMachine.ChangeState(new ScavengerIdleState(_shipController));
                return;
            }

            if (_shipCore.TimeSpentCircling > timeToSpendCirclingTillStateChange)
            {
                _stateMachine.ChangeState(new ScavengerCircleState(_shipController));
                return;
            }

            if (_currentTarget == null) 
            {
                _shipController.StateMachine.ChangeState(new ScavengerIdleState(_shipController));
                return;
            }

            Vector2 lastKnownTargetPosition = _shipCore.GetTargetPosition(
                _scavengerObject,
                _scavengerTransform.position,
                _scavengerRigid2D.velocity,
                _currentTarget.GetPosition(), 
                chaseRadius,
                whichRaycastableTargetLayers
            );

            if (lastKnownTargetPosition == Vector2.zero)
            {
                _shipController.StateMachine.ChangeState(new ScavengerIdleState(_shipController));
                return;
            }

            Vector2 weightedDirection = _shipCore.FindBestDirection(
                _scavengerObject,
                _scavengerTransform.position, 
                lastKnownTargetPosition,
                _scavengerRigid2D.velocity.magnitude,
                numberOfRays,
                collisionCheckRadius,
                whichRaycastableAvoidanceLayers
            );

            weightedDirection = _shipCore.CircleTarget(weightedDirection, _scavengerTransform.position, _scavengerRigid2D.velocity, lastKnownTargetPosition);
            bool isPlayerInSight = _shipCore.IsTargetWithinSight(_scavengerTransform.position, _scavengerTransform.up,  lastKnownTargetPosition, targetSightDistance, targetSightAngle);

            lerpVector = Vector2.Lerp(_scavengerTransform.up, weightedDirection, 0.7f).normalized;
            visualLerpVector = Vector2.Lerp(_scavengerTransform.up, weightedDirection, 0.15f);

            if (_shipCore.CanFireProjectile() && isPlayerInSight)
            {
                Vector2 firingPosition = _shipCore.GetProjectileFiringPosition(
                    _scavengerTransform.position,
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
                distance = Vector2.Distance(_scavengerTransform.position, _currentTarget.GetPosition());
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

        public void Exit()
        {

        }
    }
}
