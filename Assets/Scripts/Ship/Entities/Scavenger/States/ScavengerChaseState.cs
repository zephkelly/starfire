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
        private StateMachine _stateMachine;
        private GameObject _scavengerObject;
        private Rigidbody2D _scavengerRigid2D;
        private Transform _scavengerTransform;
        private Transform _targetTransform;
        private Rigidbody2D _targetRigid2D;

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
            _stateMachine = scavengerController.ScavengerStateMachine;
            _scavengerObject = scavengerController.ScavengerObject;
            _scavengerRigid2D = scavengerController.ScavengerRigidbody;
            _scavengerTransform = scavengerController.ScavengerTransform;
            _targetTransform = scavengerController.TargetTransform;
            _targetRigid2D = scavengerController.TargetRigidbody;
        }

        public void Enter()
        {
            if (_targetTransform.CompareTag("Friend"))
            {
                whichRaycastableAvoidanceLayers = LayerMask.GetMask("Friend"); 
            }
            else
            {
                whichRaycastableAvoidanceLayers = LayerMask.GetMask("Player");
            }

            whichRaycastableTargetLayers = LayerMask.GetMask("Friend", "Player");
            timeToSpendCirclingTillStateChange = UnityEngine.Random.Range(3f, 6f);
        }

        public void Execute()
        {
            if (_targetTransform == null)
            {
                _stateMachine.ChangeState(new ScavengerIdleState(_shipController));
                return;
            }

            if (_shipController.TimeSpentCircling > timeToSpendCirclingTillStateChange)
            {
                _stateMachine.ChangeState(new ScavengerCircleState(_shipController, _scavengerRigid2D, _targetTransform));
                return;
            }

            Vector2 lastKnownTargetPosition = _shipController.GetTargetPosition(
                _scavengerObject,
                _scavengerTransform.position,
                _scavengerRigid2D.velocity,
                _targetTransform.position, 
                chaseRadius,
                whichRaycastableTargetLayers
            );

            Vector2 weightedDirection = _shipController.FindBestDirection(
                _scavengerObject,
                _scavengerTransform.position, 
                _targetTransform.position,
                _scavengerRigid2D.velocity.magnitude,
                numberOfRays,
                collisionCheckRadius,
                whichRaycastableAvoidanceLayers
            );

            weightedDirection = _shipController.CirclePlayer(weightedDirection, _scavengerTransform.position, _scavengerRigid2D.velocity, lastKnownTargetPosition);

            lerpVector = Vector2.Lerp(_scavengerTransform.up, weightedDirection, 0.7f).normalized;
            visualLerpVector = Vector2.Lerp(_scavengerTransform.up, weightedDirection, 0.15f);

            Debug.DrawRay(_scavengerTransform.position, lerpVector.normalized * 20f, Color.green);

            bool isPlayerInSight = _shipController.IsPlayerWithinSight(_scavengerTransform.position, lastKnownTargetPosition, targetSightDistance, targetSightAngle);

            if (_shipController.CanFireProjectile() && isPlayerInSight)
            {
                Vector2 firingPosition = _shipController.GetProjectileFiringPosition(_scavengerTransform.position, lastKnownTargetPosition);
                _shipController.FireProjectileToPosition(firingPosition);
            }
        }

        public void FixedUpdate()
        {
            float distance = 0f;

            if (_targetTransform != null)
            {
                distance = Vector2.Distance(_scavengerTransform.position, _targetTransform.position);
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
