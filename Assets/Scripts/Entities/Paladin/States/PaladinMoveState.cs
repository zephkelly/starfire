using UnityEngine;

namespace Starfire
{
    public class PaladinMoveState
    {
        // private PaladinShipController _shipController;
        // private StandardAICore _shipCore;

        // private StateMachine _stateMachine;

        // private GameObject _shipObject;
        // private Rigidbody2D _shipRigidBody;
        // private Transform _shipTransform;

        // private Vector2 lerpVector;
        // private Vector2 visualLerpVector;
        // private LayerMask whichRaycastableTargetLayers;
        // private int numberOfRays = 8;

        // public PaladinMoveState(PaladinShipController controller)
        // {
        //     _shipController = controller;
        //     _shipCore = (StandardAICore)controller.AICore;

        //     _stateMachine = controller.ShipStateMachine;
        //     _shipObject = controller.ShipObject;
        //     _shipRigidBody = controller.ShipRigidBody;
        //     _shipTransform = controller.ShipTransform;
        // }

        // public void Enter()
        // {
        //     whichRaycastableTargetLayers = LayerMask.GetMask("Enemy");
        // }

        // public void Execute()
        // {
        //     // Vector2 weightedDirection = _shipCore.FindBestDirection(
        //     //     _shipObject,
        //     //     _shipTransform.position,
        //     //     _currentCommand.GetTargetPosition(),
        //     //     _shipRigidBody.velocity.magnitude,
        //     //     numberOfRays,
        //     //     whichRaycastableTargetLayers
        //     // );

        //     // weightedDirection = _shipCore.CircleTarget(
        //     //     weightedDirection,
        //     //     _shipTransform.position,
        //     //     _shipRigidBody.velocity,
        //     //     _currentCommand.GetTargetPosition()
        //     // );

        //     // lerpVector = Vector2.Lerp(_shipTransform.up, weightedDirection, 0.7f).normalized;
        //     // visualLerpVector = Vector2.Lerp(_shipTransform.up, weightedDirection, 0.15f);
            
        // }

        // public void FixedUpdate()
        // {
        //     _shipController.MoveInDirection(lerpVector, _shipController.Ship.Configuration.ThrusterMaxSpeed, true);
        //     _shipController.RotateToDirection(visualLerpVector, _shipController.Ship.Configuration.TurnDegreesPerSecond);
        // }

        // public void Exit()
        // {
        //     // _shipController.SetThrusters(false, Vector2.zero, false);
        // }
    }
}