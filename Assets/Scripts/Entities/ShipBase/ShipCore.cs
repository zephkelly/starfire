using Unity.Mathematics;
using UnityEngine;

namespace Starfire
{
    public abstract class ShipCore
    {
        protected Target currentTarget;
        
        public Target CurrentTarget => currentTarget;

        /// <summary>
        /// Set a new current target for the ship. Will not set the same target as current target.
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