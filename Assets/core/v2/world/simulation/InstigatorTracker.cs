using UnityEngine;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Tracks who last pushed/collided with this object.
    /// Attach to asteroids and other objects that should track blame for collisions.
    /// </summary>
    public class InstigatorTracker : MonoBehaviour
    {
        /// <summary>
        /// Entity ID of whoever last pushed or collided with this object.
        /// Null if no tracked collision has occurred.
        /// </summary>
        public int? InstigatorEntityId { get; private set; }

        /// <summary>
        /// Type of the instigator entity.
        /// </summary>
        public EntityType? InstigatorType { get; private set; }

        /// <summary>
        /// Time when the last instigator collision occurred.
        /// </summary>
        public float LastInstigatorTime { get; private set; }

        /// <summary>
        /// Minimum relative velocity for a collision to be tracked as instigation.
        /// </summary>
        [SerializeField]
        private float minInstigationVelocity = 0.5f;

        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryTrackInstigator(collision.gameObject, collision.relativeVelocity.magnitude);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // For trigger-based collisions, estimate relative velocity
            var otherRb = other.attachedRigidbody;
            if (otherRb != null && _rb != null)
            {
                float relVel = (otherRb.linearVelocity - _rb.linearVelocity).magnitude;
                TryTrackInstigator(other.gameObject, relVel);
            }
        }

        private void TryTrackInstigator(GameObject other, float relativeVelocity)
        {
            // Only track significant collisions
            if (relativeVelocity < minInstigationVelocity)
                return;

            // Try to find an entity on the colliding object
            var entity = FindEntityOnObject(other);
            if (entity != null)
            {
                SetInstigator(entity.Id, entity.EntityType);
            }
        }

        private IEntity FindEntityOnObject(GameObject obj)
        {
            // Check the object itself
            var entity = obj.GetComponent<IEntity>();
            if (entity != null) return entity;

            // Check parent (entity might be on root)
            if (obj.transform.parent != null)
            {
                entity = obj.GetComponentInParent<IEntity>();
                if (entity != null) return entity;
            }

            return null;
        }

        private void TrySetInstigatorFromEntity(IEntity entity)
        {
            if (entity != null)
            {
                SetInstigator(entity.Id, entity.EntityType);
            }
        }

        /// <summary>
        /// Manually set the instigator (for projectile hits, etc.).
        /// </summary>
        public void SetInstigator(int entityId, EntityType entityType)
        {
            InstigatorEntityId = entityId;
            InstigatorType = entityType;
            LastInstigatorTime = Time.time;
        }

        /// <summary>
        /// Clear the tracked instigator.
        /// </summary>
        public void ClearInstigator()
        {
            InstigatorEntityId = null;
            InstigatorType = null;
        }
    }
}
