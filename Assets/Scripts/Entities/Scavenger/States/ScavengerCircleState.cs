using UnityEngine;

namespace Starfire
{
    public class ScavengerCircleState
    {
        // private ScavengerShipController _shipController;
        // private StandardAICore _shipCore;

        // private StateMachine _stateMachine;
        // private GameObject _scavengerObject;
        // private Rigidbody2D _scavengerRigid2D;
        // private Transform _scavengerTransform;

        // private LayerMask whichRaycastableLayers;
        // private Vector2 lastKnownPlayerPosition;
        // private Vector2 weightedDirection;
        // private Vector2 lerpVector;
        // private Vector2 visualLerpVector;
        // private int numberOfRays = 16;
        // private float chaseRadius = 300f;
        // private float collisionCheckRadius = 12f;
        // private float targetSightDistance = 1000f;
        // private float targetSightAngle = 90f;

        // private MovementPattern currentMovementPattern = MovementPattern.Normal;
        // private CirclePattern currentCirclePattern = CirclePattern.Clockwise;
        // private float timeTillCirclePatternChange = 2f;
        // private float timeTillMovePatternChange = 3f;

        // private enum CirclePattern
        // {
        //     Clockwise,
        //     CounterClockwise,
        // }

        // private enum MovementPattern
        // {
        //     Normal,
        //     ZigZag,
        //     Fixed,
        //     FigureEight
        // }

        // public ScavengerCircleState(ScavengerShipController controller)
        // {
        //     _shipController = controller;
        //     _shipCore = (StandardAICore)controller.AICore;

        //     _stateMachine = controller.ShipStateMachine;
        //     _scavengerObject = controller.ShipObject;
        //     _scavengerRigid2D = controller.ShipRigidBody;
        //     _scavengerTransform = controller.ShipTransform;
        // }

        // public void Enter()
        // {
        //     timeTillCirclePatternChange = Random.Range(4f, 8f);
        //     timeTillMovePatternChange = Random.Range(4f, 8f);
        //     whichRaycastableLayers = LayerMask.GetMask("Player", "Friend");
        // }

        // public void Execute()
        // {
        //     if (_shipCore.TimeSpentNotCircling > 4f)
        //     {
        //         _stateMachine.ChangeState(new ScavengerChaseState(_shipController));
        //     }

            // lastKnownPlayerPosition = _shipCore.GetTargetPosition(
            //     _scavengerObject,
            //     _scavengerTransform.position,
            //     _scavengerRigid2D.velocity,
            //     _currentCommand.GetTargetPosition(), 
            //     whichRaycastableLayers,
            //     chaseRadius
            // );

            // weightedDirection = _shipCore.FindBestDirection(
            //     _scavengerObject,
            //     _scavengerTransform.position,
            //     lastKnownPlayerPosition,
            //     _scavengerRigid2D.velocity.magnitude,
            //     numberOfRays,
            //     whichRaycastableLayers,
            //     collisionCheckRadius
            // );

            // weightedDirection = _shipCore.CircleTarget(weightedDirection, _scavengerTransform.position, _scavengerRigid2D.velocity, lastKnownPlayerPosition);

            // lerpVector = Vector2.Lerp(_scavengerTransform.up, AdjustLerpPattern(weightedDirection), 0.7f).normalized;
            // visualLerpVector = Vector2.Lerp(_scavengerTransform.up, AdjustVisualLerpPattern(weightedDirection), 0.15f);

            // currentCirclePattern = GetRandomCirclePattern();
            // currentMovementPattern = GetRandomMovementPattern();

            // bool isPlayerInSight = _shipCore.IsTargetWithinSight(_scavengerTransform.position, _scavengerTransform.up, lastKnownPlayerPosition, targetSightDistance, targetSightAngle);

            // if (_shipCore.CanFireProjectile() && isPlayerInSight)
            // {
            //     Vector2 firingPosition = _shipCore.GetProjectileFiringPosition(
            //         _scavengerTransform.position,
            //         lastKnownPlayerPosition
            //     );
            //     _shipController.FireProjectileToPosition(firingPosition);
            // }
        // }

        // public void FixedUpdate()
        // {
        //     _shipController.MoveInDirection(lerpVector, GetShipSpeed(), true);
        //     _shipController.RotateToDirection(visualLerpVector, _shipController.Ship.Configuration.TurnDegreesPerSecond);
        // } 

        // private Vector2 AdjustLerpPattern(Vector2 _weightedDirection)
        // {
        //     Vector2 newLerpVector = _weightedDirection;

        //     if (currentMovementPattern == MovementPattern.Normal)
        //     {
        //         newLerpVector = _weightedDirection;
        //     }
        //     else if (currentMovementPattern == MovementPattern.ZigZag)
        //     {
        //         newLerpVector = _weightedDirection + (Vector2.Perpendicular(_weightedDirection).normalized * Mathf.Sin(Time.time * 3f) * 0.8f);
        //     }

        //     return newLerpVector;
        // }

        // private Vector2 AdjustVisualLerpPattern(Vector2 _weightedDirection)
        // {
        //     Vector2 newVisualLerpVector = AdjustLerpPattern(_weightedDirection);

        //     if (currentMovementPattern == MovementPattern.Fixed)
        //     {
        //         newVisualLerpVector = (lastKnownPlayerPosition - (Vector2)_scavengerTransform.position).normalized;
        //     }

        //     return newVisualLerpVector;
        // }

        // private float GetShipSpeed()
        // {
        //     float shipSpeed = _shipController.Ship.Configuration.ThrusterMaxSpeed;

        //     if (currentMovementPattern == MovementPattern.Fixed)
        //     {
        //         shipSpeed = shipSpeed * 0.5f;
        //     }

        //     return shipSpeed;
        // }

        // private CirclePattern GetRandomCirclePattern()
        // {
        //     CirclePattern newCirclePattern = currentCirclePattern;
        //     timeTillCirclePatternChange -= Time.deltaTime;

        //     if (timeTillCirclePatternChange <= 0)
        //     {
        //         newCirclePattern = (CirclePattern)Random.Range(0, 2);
        //         timeTillCirclePatternChange = Random.Range(4f, 8f);
        //     }

        //     return newCirclePattern;
        // }

        // private MovementPattern GetRandomMovementPattern()
        // {
        //     MovementPattern newMovementPattern = currentMovementPattern;
        //     timeTillMovePatternChange -= Time.deltaTime;

        //     if (timeTillMovePatternChange <= 0)
        //     {
        //         timeTillMovePatternChange = Random.Range(4f, 8f);
        //         int randomMovementPattern = Random.Range(0, 10);

        //         if (randomMovementPattern <= 2)
        //         {
        //             newMovementPattern = MovementPattern.Fixed;
        //         }
        //         else if (randomMovementPattern <= 4)
        //         {
        //             newMovementPattern = MovementPattern.ZigZag;
        //         }
        //         else
        //         {
        //             newMovementPattern = MovementPattern.Normal;
        //         }
        //     }

        //     return newMovementPattern;
        // }

        // public void Exit() { }
    }
}