using Unity.Mathematics;
using UnityEngine;

namespace Starfire
{
    public abstract class ShipCore
    {
        protected Target currentTarget = null;
        
        public Target CurrentTarget => currentTarget;

        /// <summary>
        /// Set a new current target Transform for the ship. Will not set the same target as current target.
        /// </summary>
        /// <param name="newTarget"></param>
        /// <returns>True if the target was updated, False if the target was not updated.</returns>
        public bool SetTarget(GameObject newTarget) 
        {
            Transform newTransform = newTarget.transform;
            if (currentTarget is not null && currentTarget.IsSameTargetAs(newTransform)) return false;

            currentTarget = new Target(newTransform, newTarget.GetComponent<Rigidbody2D>());
            return true;
        }

        /// <summary>
        /// Set a new current target Position for the ship. Will not set the same target as current target.
        /// </summary>
        /// <param name="newTargetPosition"></param>
        /// <returns>True if the target was updated, False if the target was not updated.</returns>
        public bool SetTarget(Vector2 newTargetPosition) 
        {
            if (currentTarget is not null && currentTarget.IsSameTargetAs(newTargetPosition)) return false;
            currentTarget = new Target(newTargetPosition);
            return true;
        }

        public bool RemoveTarget()
        {
            if (currentTarget is null) return false;

            currentTarget = null;
            return true;
        }

        public void Update()
        {
            if (currentTarget != null && currentTarget.IsDestroyed())
            {
                RemoveTarget();
            }
        }
    }
}