using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Marks a Transform as a rotational thruster mount point.
    /// Place on child GameObjects of ships to define thruster positions visually in the scene.
    /// The slotId must match a ThrusterDefinition in the ShipRotationModuleConfig.
    /// </summary>
    public class ThrusterMarker : MonoBehaviour
    {
        [Tooltip("Must match the slotId in ThrusterDefinition")]
        [SerializeField] private string slotId;

        [Tooltip("Thrust direction in local space (normalized)")]
        [SerializeField] private Vector2 thrustDirection = Vector2.up;

        [Tooltip("Optional: Override visual prefab for this specific thruster")]
        [SerializeField] private GameObject visualPrefabOverride;

        /// <summary>
        /// Unique identifier matching ThrusterDefinition.slotId.
        /// </summary>
        public string SlotId => slotId;

        /// <summary>
        /// Thrust direction in local space.
        /// </summary>
        public Vector2 ThrustDirection => thrustDirection.normalized;

        /// <summary>
        /// Optional visual prefab override for this thruster.
        /// </summary>
        public GameObject VisualPrefabOverride => visualPrefabOverride;

        /// <summary>
        /// The transform to use as the thruster mount point.
        /// </summary>
        public Transform MountPoint => transform;

        /// <summary>
        /// World-space thrust direction based on current transform rotation.
        /// </summary>
        public Vector2 WorldThrustDirection =>
            transform.TransformDirection(thrustDirection).normalized;

        /// <summary>
        /// World-space position of the thruster.
        /// </summary>
        public Vector2 WorldPosition => transform.position;

        private void OnDrawGizmosSelected()
        {
            // Draw thruster position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.08f);

            // Draw thrust direction
            Gizmos.color = Color.cyan;
            Vector3 worldDir = transform.TransformDirection(thrustDirection.normalized);
            Gizmos.DrawRay(transform.position, worldDir * 0.4f);

            // Draw arrow head
            Vector3 arrowEnd = transform.position + worldDir * 0.4f;
            Vector3 arrowLeft = Quaternion.Euler(0, 0, 150) * worldDir * 0.1f;
            Vector3 arrowRight = Quaternion.Euler(0, 0, -150) * worldDir * 0.1f;
            Gizmos.DrawRay(arrowEnd, arrowLeft);
            Gizmos.DrawRay(arrowEnd, arrowRight);
        }

        private void OnDrawGizmos()
        {
            // Subtle indicator when not selected
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    }
}
