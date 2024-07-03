using UnityEngine;

namespace Starfire
{
    public interface IStandardAI
    {
        // Weaponry
        bool CanFireProjectile();
        Vector2 GetProjectileFiringPosition(Vector2 thisShipPosition, Vector2 targetShipPosition);
        // Movement
        bool IsTargetWithinSight(Vector2 thisShipPosition, Vector2 targetShipPosition, float sightDistance, float maximumFireAngle);
        Vector2 GetTargetPosition(GameObject thisShipObject, Vector2 thisShipPosition, Vector2 thisShipVelocity, Vector2 targetShipPosition, float chaseRadius, LayerMask whichRaycastableLayers);
        Vector2 FindBestDirection(GameObject thisShipObject, Vector2 thisShipPosition, Vector2 lastTargetShipPosition, float ourShipVelocityMagnitude, int numberOfRays, float collisionCheckRadius, LayerMask whichRaycastableLayers);
        Vector2 CircleTarget(Vector2 weightedDirection, Vector2 ourShipPosition, Vector2 ourShipVelocity, Vector2 targetShipPosition);
    }
}