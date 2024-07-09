using UnityEngine;

namespace Starfire
{
    public interface IAICore
    {
        void Update();
        void FixedUpdate();
        void SetShip(Ship ship);
        void SetFleet(Fleet fleet);
        void SetBlackboard(Blackboard blackboard);
        void CreateBehaviourTree();
        void SetTarget(Ship target);
        void SetTarget(Vector2 target);
        void SetTarget(Transform target);
        Vector2 CalculateAvoidanceSteeringDirection(GameObject ourShipObject, Vector2 ourShipPosition, float ourShipVelocityMagnitude, Vector2 currentDirection, LayerMask whichRaycastableLayers, int numberOfRays, float collisionCheckRadius = 30f);
    }
}