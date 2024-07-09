using UnityEngine;

namespace Starfire
{
    public class PaladinChaseState
    {
        // private PaladinShipController _shipController;
        // private StandardAICore _shipCore;

        // private StateMachine _stateMachine;
        // private GameObject _paladinObject;
        // private Rigidbody2D _paladinRigid2D;
        // private Transform _paladinTransform;

        // private Vector2 lerpVector;
        // private Vector2 visualLerpVector;

        // private LayerMask whichRaycastableAvoidanceLayers;
        // private LayerMask whichRaycastableTargetLayers;
        // private int numberOfRays = 16;
        // private float chaseRadius = 300f;
        // private float collisionCheckRadius = 30f;
        // private float targetSightDistance = 1000f;
        // private float targetSightAngle = 90f;

        // private float timeToSpendCirclingTillStateChange;

        // public PaladinChaseState(PaladinShipController paladinController)
        // {
        //     _shipController = paladinController;
        //     _shipCore = (StandardAICore)_shipController.AICore;

        //     _stateMachine = _shipController.ShipStateMachine;
        //     _paladinObject = _shipController.ShipObject;
        //     _paladinRigid2D = _shipController.ShipRigidBody;
        //     _paladinTransform = _shipController.ShipTransform;
        // }

        // public void Enter()
        // {
        //     whichRaycastableTargetLayers = LayerMask.GetMask("Enemy");
        //     timeToSpendCirclingTillStateChange = UnityEngine.Random.Range(3f, 6f);
        // }

        // public void Execute()
        // {
        //     if (_shipCore.TimeSpentCircling > timeToSpendCirclingTillStateChange)
        //     {
        //         _stateMachine.ChangeState(new PaladinCircleState(_shipController));
        //         return;
        //     }

        //     // Vector2 lastKnownTargetPosition = _shipCore.GetTargetPosition(
        //     //     _paladinObject,
        //     //     _paladinTransform.position,
        //     //     _paladinRigid2D.velocity,
        //     //     _currentCommand.GetTargetPosition(), 
        //     //     whichRaycastableTargetLayers,
        //     //     chaseRadius
        //     // );

        //     // Vector2 weightedDirection = _shipCore.FindBestDirection(
        //     //     _paladinObject,
        //     //     _paladinTransform.position, 
        //     //     _currentCommand.GetTargetPosition(),
        //     //     _paladinRigid2D.velocity.magnitude,
        //     //     numberOfRays,
        //     //     whichRaycastableAvoidanceLayers,
        //     //     collisionCheckRadius
        //     // );

        //     // weightedDirection = _shipCore.CircleTarget(weightedDirection, _paladinTransform.position, _paladinRigid2D.velocity, lastKnownTargetPosition);
        //     // bool isPlayerInSight = _shipCore.IsTargetWithinSight(_paladinTransform.position, _paladinTransform.up,  lastKnownTargetPosition, targetSightDistance, targetSightAngle);

        //     // lerpVector = Vector2.Lerp(_paladinTransform.up, weightedDirection, 0.7f).normalized;
        //     // visualLerpVector = Vector2.Lerp(_paladinTransform.up, weightedDirection, 0.15f);

        //     // if (_shipCore.CanFireProjectile() && isPlayerInSight)
        //     // {
        //     //     Vector2 firingPosition = _shipCore.GetProjectileFiringPosition(
        //     //         _paladinTransform.position,
        //     //         lastKnownTargetPosition
        //     //     );
        //     //     _shipController.FireProjectileToPosition(firingPosition);
        //     // }
        // }

        // public void FixedUpdate()
        // {
        //     float distance = 0f;
        //     float speedMultiplier = GetSpeedMultiplier(distance);
        //     float speed = _shipController.Ship.Configuration.ThrusterMaxSpeed * speedMultiplier;

        //     _shipController.MoveInDirection(lerpVector, speed, true);
        //     _shipController.RotateToDirection(visualLerpVector, _shipController.Ship.Configuration.TurnDegreesPerSecond);
        // }

        // private float GetSpeedMultiplier(float distance)
        // {
        //     float minSpeedMultiplier = 0.8f;
        //     float maxSpeedMultiplier = 1.1f;
        //     return Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, Mathf.InverseLerp(0, 220, distance));
        // }

        // public void Exit() { }
    }
}
