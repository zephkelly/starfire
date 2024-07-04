using UnityEngine;

namespace Starfire
{
    public class PaladinCircleState : IState
    {
        private PaladinShipController _shipController;
        private StandardAICore _shipCore;
        private Target _currentTarget;

        private Rigidbody2D _paladinRigid2D;
        private Transform _paladinTransform;

        private LayerMask whichRaycastableLayers;
        private Vector2 lastKnowTargetPosition;
        private Vector2 weightedDirection;
        private Vector2 lerpVector;
        private Vector2 visualLerpVector;
        private int numberOfRays = 16;
        private float chaseRadius = 300f;
        private float collisionCheckRadius = 12f;
        private float targetSightDistance = 200f;
        private float targetSightAngle = 90f;

        private MovementPattern currentMovementPattern = MovementPattern.Normal;
        private CirclePattern currentCirclePattern = CirclePattern.Clockwise;
        private float timeTillCirclePatternChange = 2f;
        private float timeTillMovePatternChange = 3f;

        private enum CirclePattern
        {
            Clockwise,
            CounterClockwise,
        }

        private enum MovementPattern
        {
            Normal,
            ZigZag,
            Fixed,
            FigureEight
        }

        public PaladinCircleState(PaladinShipController controller)
        {
            _shipController = controller;
            _shipCore = (StandardAICore)controller.ShipCore;
            _currentTarget = controller.ShipCore.CurrentTarget;
            _paladinRigid2D = _shipController.ShipRigidBody;
            _paladinTransform = _shipController.ShipTransform;
        }

        public void Enter()
        {
            timeTillCirclePatternChange = Random.Range(4f, 8f);
            timeTillMovePatternChange = Random.Range(4f, 8f);
            whichRaycastableLayers = LayerMask.GetMask("Enemy");
        }

        public void Execute()
        {
            if (_currentTarget == null)
            {
                _shipController.StateMachine.ChangeState(new PaladinIdleState(_shipController));
                return;
            }

            if (_shipCore.TimeSpentNotCircling > 4f)
            {
                _shipController.StateMachine.ChangeState(new PaladinChaseState(_shipController));
            }

            if (_currentTarget == null) 
            {
                _shipController.StateMachine.ChangeState(new PaladinIdleState(_shipController));
                return;
            }

            lastKnowTargetPosition = _shipCore.GetTargetPosition(
                _shipController.ShipObject,
                _paladinTransform.position,
                _paladinRigid2D.velocity,
                _currentTarget.GetPosition(),
                chaseRadius,
                whichRaycastableLayers
            );

            weightedDirection = _shipCore.FindBestDirection(
                _shipController.ShipObject,
                _paladinTransform.position,
                lastKnowTargetPosition,
                _paladinRigid2D.velocity.magnitude,
                numberOfRays,
                collisionCheckRadius,
                whichRaycastableLayers
            );

            weightedDirection = _shipCore.CircleTarget(weightedDirection, _paladinTransform.position, _paladinRigid2D.velocity, lastKnowTargetPosition);

            lerpVector = Vector2.Lerp(_paladinTransform.up, AdjustLerpPattern(weightedDirection), 0.7f).normalized;
            visualLerpVector = Vector2.Lerp(_paladinTransform.up, AdjustVisualLerpPattern(weightedDirection), 0.15f);

            Debug.DrawRay(_paladinTransform.position, lerpVector.normalized * 10f, Color.red);

            currentCirclePattern = GetRandomCirclePattern();
            currentMovementPattern = GetRandomMovementPattern();

            bool isPlayerInSight = _shipCore.IsTargetWithinSight(_paladinTransform.position, _paladinTransform.up, lastKnowTargetPosition, targetSightDistance, targetSightAngle);

            if (_shipCore.CanFireProjectile() && isPlayerInSight)
            {
                Vector2 firingPosition = _shipCore.GetProjectileFiringPosition(
                    _paladinTransform.position,
                    lastKnowTargetPosition
                );
                _shipController.FireProjectileToPosition(firingPosition);
            }
        }

        public void FixedUpdate()
        {
            _shipController.MoveInDirection(lerpVector, GetShipSpeed(), true);
            _shipController.RotateToDirection(visualLerpVector, _shipController.Ship.Configuration.TurnDegreesPerSecond);
        } 

        private Vector2 AdjustLerpPattern(Vector2 _weightedDirection)
        {
            Vector2 newLerpVector = _weightedDirection;

            if (currentMovementPattern == MovementPattern.Normal)
            {
                newLerpVector = _weightedDirection;
            }
            else if (currentMovementPattern == MovementPattern.ZigZag)
            {
                newLerpVector = _weightedDirection + (Vector2.Perpendicular(_weightedDirection).normalized * Mathf.Sin(Time.time * 3f) * 0.8f);
            }

            return newLerpVector;
        }

        private Vector2 AdjustVisualLerpPattern(Vector2 _weightedDirection)
        {
            Vector2 newVisualLerpVector = AdjustLerpPattern(_weightedDirection);

            if (currentMovementPattern == MovementPattern.Fixed)
            {
                newVisualLerpVector = (lastKnowTargetPosition - (Vector2)_paladinTransform.position).normalized;
            }

            return newVisualLerpVector;
        }

        private float GetShipSpeed()
        {
            float shipSpeed = _shipController.Ship.Configuration.ThrusterMaxSpeed;

            if (currentMovementPattern == MovementPattern.Fixed)
            {
                shipSpeed = shipSpeed * 0.5f;
            }

            return shipSpeed;
        }

        private CirclePattern GetRandomCirclePattern()
        {
            CirclePattern newCirclePattern = currentCirclePattern;
            timeTillCirclePatternChange -= Time.deltaTime;

            if (timeTillCirclePatternChange <= 0)
            {
                newCirclePattern = (CirclePattern)Random.Range(0, 2);
                timeTillCirclePatternChange = Random.Range(4f, 8f);
            }

            return newCirclePattern;
        }

        private MovementPattern GetRandomMovementPattern()
        {
            MovementPattern newMovementPattern = currentMovementPattern;
            timeTillMovePatternChange -= Time.deltaTime;

            if (timeTillMovePatternChange <= 0)
            {
                timeTillMovePatternChange = Random.Range(4f, 8f);
                int randomMovementPattern = Random.Range(0, 10);

                if (randomMovementPattern <= 2)
                {
                    newMovementPattern = MovementPattern.Fixed;
                }
                else if (randomMovementPattern <= 4)
                {
                    newMovementPattern = MovementPattern.ZigZag;
                }
                else
                {
                    newMovementPattern = MovementPattern.Normal;
                }
            }

            return newMovementPattern;
        }

        public void Exit() { }
    }
}