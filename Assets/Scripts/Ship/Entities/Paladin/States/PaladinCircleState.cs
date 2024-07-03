using UnityEngine;

namespace Starfire
{
    public class PaladinCircleState : IState
    {
        private PaladinShipController _shipController;
        private Rigidbody2D _paladinRigid2D;
        private Transform _pladinTransform;
        private Transform _targetTransform;

        private Vector2 lastKnownPlayerPosition;
        private Vector2 weightedDirection;
        private LayerMask whichRaycastableLayers;
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

        public PaladinCircleState(PaladinShipController controller, Rigidbody2D rigid2D, Transform target)
        {
            _shipController = controller;
            _pladinTransform = controller.transform;
            _paladinRigid2D = rigid2D;
            _targetTransform = target;
        }

        public void Enter()
        {
            timeTillCirclePatternChange = Random.Range(4f, 8f);
            timeTillMovePatternChange = Random.Range(4f, 8f);
            whichRaycastableLayers = LayerMask.GetMask("Enemy");
        }

        public void Execute()
        {
            if (_targetTransform == null)
            {
                _shipController.ScavengerStateMachine.ChangeState(new PaladinIdleState(_shipController));
                return;
            }

            if (_shipController.TimeSpentNotCircling > 4f)
            {
                _shipController.ScavengerStateMachine.ChangeState(new PaladinChaseState(_shipController));
            }

            lastKnownPlayerPosition = _shipController.GetTargetPosition(
                _shipController.ScavengerObject,
                _pladinTransform.position,
                _paladinRigid2D.velocity,
                _targetTransform.position,
                chaseRadius,
                whichRaycastableLayers
            );

            weightedDirection = _shipController.FindBestDirection(
                _shipController.ScavengerObject,
                _pladinTransform.position,
                lastKnownPlayerPosition,
                _paladinRigid2D.velocity.magnitude,
                numberOfRays,
                collisionCheckRadius,
                whichRaycastableLayers
            );

            weightedDirection = _shipController.CirclePlayer(weightedDirection, _pladinTransform.position, _paladinRigid2D.velocity, lastKnownPlayerPosition);

            lerpVector = Vector2.Lerp(_pladinTransform.up, AdjustLerpPattern(weightedDirection), 0.7f).normalized;
            visualLerpVector = Vector2.Lerp(_pladinTransform.up, AdjustVisualLerpPattern(weightedDirection), 0.15f);

            Debug.DrawRay(_pladinTransform.position, lerpVector.normalized * 10f, Color.red);

            currentCirclePattern = GetRandomCirclePattern();
            currentMovementPattern = GetRandomMovementPattern();

            bool isPlayerInSight = _shipController.IsPlayerWithinSight(_pladinTransform.position, lastKnownPlayerPosition, targetSightDistance, targetSightAngle);

            if (_shipController.CanFireProjectile() && isPlayerInSight)
            {
                Vector2 firingPosition = _shipController.GetProjectileFiringPosition(_pladinTransform.position, lastKnownPlayerPosition);
                _shipController.FireProjectileToPosition(firingPosition);
            }
        }

        public void FixedUpdate()
        {
            _shipController.MoveInDirection(lerpVector, GetShipSpeed(), true);
            _shipController.RotateToDirection(visualLerpVector, _shipController.Configuration.TurnDegreesPerSecond);
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
                newVisualLerpVector = (lastKnownPlayerPosition - (Vector2)_pladinTransform.position).normalized;
            }

            return newVisualLerpVector;
        }

        private float GetShipSpeed()
        {
            float shipSpeed = _shipController.Configuration.ThrusterMaxSpeed;

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

        public void Exit()
        {
        }
    }
}