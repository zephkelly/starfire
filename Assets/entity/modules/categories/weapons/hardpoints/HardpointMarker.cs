using UnityEngine;

namespace Starfire.Entity.Modules.Weapon
{
    /// <summary>
    /// Marks a Transform as a weapon hardpoint.
    /// Place on child GameObjects of ships to define weapon mount positions.
    /// The slotId must match a weapon slot defined in the ShipClassDefinition.
    /// </summary>
    public class HardpointMarker : MonoBehaviour
    {
        [Tooltip("Must match the slotId in ShipClassDefinition's MultiSlotEntry")]
        [SerializeField] private string slotId;

        [Tooltip("Forward direction for fixed weapons (local space)")]
        [SerializeField] private Vector2 firingDirection = Vector2.up;

        [Tooltip("Firing arc in degrees (0 = fixed forward, 360 = full rotation)")]
        [Range(0f, 360f)]
        [SerializeField] private float firingArc = 0f;

        public string SlotId => slotId;
        public Vector2 FiringDirection => firingDirection;
        public float FiringArc => firingArc;
        public Transform MountPoint => transform;

        /// <summary>
        /// World-space firing direction based on current transform rotation.
        /// </summary>
        public Vector2 WorldFiringDirection =>
            transform.TransformDirection(firingDirection).normalized;

        /// <summary>
        /// Check if a target direction is within the firing arc.
        /// </summary>
        public bool IsWithinFiringArc(Vector2 targetDirection)
        {
            if (firingArc >= 360f) return true;
            if (firingArc <= 0f) return false;

            float angle = Vector2.Angle(WorldFiringDirection, targetDirection);
            return angle <= firingArc * 0.5f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 worldDir = transform.TransformDirection(firingDirection);
            Gizmos.DrawRay(transform.position, worldDir * 0.5f);

            if (firingArc > 0f && firingArc < 360f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                float halfArc = firingArc * 0.5f;
                Vector3 leftDir = Quaternion.Euler(0, 0, halfArc) * worldDir;
                Vector3 rightDir = Quaternion.Euler(0, 0, -halfArc) * worldDir;
                Gizmos.DrawRay(transform.position, leftDir * 0.4f);
                Gizmos.DrawRay(transform.position, rightDir * 0.4f);
            }
        }
    }
}
