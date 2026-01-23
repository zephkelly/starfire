using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Marks a Transform as a weapon hardpoint on a ship.
    /// Contains the slot ID for matching with module slots and the weight class for compatibility.
    /// </summary>
    public class V2HardpointMarker : MonoBehaviour
    {
        [Header("Identification")]
        [Tooltip("Unique identifier matching the module slot ID in ship configuration.")]
        [SerializeField] private string slotId;

        [Header("Weight Class")]
        [Tooltip("The weight class of this hardpoint. Determines which weapons can mount here.")]
        [SerializeField] private WeaponWeightClass weightClass = WeaponWeightClass.Medium;

        [Header("Firing Direction")]
        [Tooltip("Local-space firing direction. Default is up (forward).")]
        [SerializeField] private Vector2 firingDirection = Vector2.up;

        [Tooltip("Maximum firing arc in degrees. 0 = fixed forward, 360 = full rotation.")]
        [SerializeField, Range(0f, 360f)] private float firingArc = 0f;

        /// <summary>
        /// The unique slot identifier for this hardpoint.
        /// </summary>
        public string SlotId => slotId;

        /// <summary>
        /// The weight class of this hardpoint.
        /// </summary>
        public WeaponWeightClass WeightClass => weightClass;

        /// <summary>
        /// The local-space firing direction.
        /// </summary>
        public Vector2 FiringDirection => firingDirection;

        /// <summary>
        /// The maximum firing arc in degrees.
        /// </summary>
        public float FiringArc => firingArc;

        /// <summary>
        /// The transform where weapons are mounted.
        /// </summary>
        public Transform MountPoint => transform;

        /// <summary>
        /// The firing direction in world space.
        /// </summary>
        public Vector2 WorldFiringDirection =>
            transform.TransformDirection(firingDirection).normalized;

        /// <summary>
        /// Checks if a weapon of the given weight class can mount on this hardpoint.
        /// Weapons can mount on equal or larger hardpoints.
        /// </summary>
        /// <param name="weaponClass">The weight class of the weapon to mount.</param>
        /// <returns>True if the weapon can mount, false otherwise.</returns>
        public bool CanMount(WeaponWeightClass weaponClass)
        {
            return (int)weaponClass <= (int)weightClass;
        }

        /// <summary>
        /// Checks if a target direction is within the hardpoint's firing arc.
        /// </summary>
        /// <param name="targetWorldDirection">The target direction in world space.</param>
        /// <returns>True if within the firing arc, false otherwise.</returns>
        public bool IsWithinFiringArc(Vector2 targetWorldDirection)
        {
            if (firingArc >= 360f) return true;
            if (firingArc <= 0f)
            {
                // Fixed direction - check exact alignment
                float angle = Vector2.Angle(WorldFiringDirection, targetWorldDirection);
                return angle < 1f;
            }

            float halfArc = firingArc / 2f;
            float angleToTarget = Vector2.Angle(WorldFiringDirection, targetWorldDirection);
            return angleToTarget <= halfArc;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw mount point
            Gizmos.color = GetWeightClassColor();
            Gizmos.DrawWireSphere(transform.position, 0.15f);

            // Draw firing direction
            Vector2 worldDir = WorldFiringDirection;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, worldDir * 0.5f);

            // Draw firing arc
            if (firingArc > 0f && firingArc < 360f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                float halfArc = firingArc / 2f;

                // Draw arc boundaries
                Vector2 leftBound = Quaternion.Euler(0, 0, halfArc) * worldDir;
                Vector2 rightBound = Quaternion.Euler(0, 0, -halfArc) * worldDir;

                Gizmos.DrawRay(transform.position, leftBound * 0.4f);
                Gizmos.DrawRay(transform.position, rightBound * 0.4f);
            }
        }

        private Color GetWeightClassColor()
        {
            return weightClass switch
            {
                WeaponWeightClass.Light => Color.green,
                WeaponWeightClass.Medium => Color.yellow,
                WeaponWeightClass.Heavy => Color.red,
                _ => Color.white
            };
        }
#endif
    }
}
