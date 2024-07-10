using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public interface IAICore
    {
        Blackboard Blackboard { get; }

        void Update();
        void SetShip(Ship ship);
        void SetFleet(Fleet fleet);
        void SetFleetBlackboard(FleetBlackboard blackboard);
        void CreateBehaviourTree();
        void SetTarget(Ship target);
        void SetTarget(Vector2 target);
        void SetTarget(Transform target);
        Vector2 CalculateAvoidanceSteeringDirection(GameObject ourShipObject, Vector2 ourShipPosition, float ourShipVelocityMagnitude, Vector2 currentDirection, LayerMask whichRaycastableLayers, int numberOfRays, float collisionCheckRadius = 30f);
        Vector2 AddCircleTargetBias(Vector2 weightedDirection, Vector2 ourShipPosition, Vector2 ourShipVelocity, Vector2 targetShipPosition, float orbitDistance, int orbitDirection);
    }
}