using UnityEngine;
using Starfire.Core;
using Starfire.Entity.Modules;
using Starfire.Entity.Modules.Shield;
using Starfire.Entity.Modules.Weapon;

namespace Starfire.Entity
{
    [RequireComponent(typeof(ShipController))]
    [RequireComponent(typeof(OldInputProvider))]
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Ship Class")]
        [SerializeField] private ShipClassDefinition shipClass;

        private PlayerDriver playerDriver;

        private void Start()
        {
            // Assign to Player layer for collision filtering
            gameObject.layer = LayerMask.NameToLayer(GameLayers.Player);

            var shipController = GetComponent<ShipController>();
            var inputProvider = GetComponent<OldInputProvider>();

            if (shipClass == null)
            {
                Debug.LogError("PlayerSetup: No ShipClassDefinition assigned!");
                return;
            }

            // Initialize hardpoint registry before ship systems (so weapons can find their hardpoints)
            var hardpointRegistry = GetComponent<HardpointRegistry>();
            hardpointRegistry?.Initialize();

            shipController.Initialize(shipClass);

            playerDriver = new PlayerDriver(inputProvider, priority: 10);
            shipController.DriverStack.Push(playerDriver);
        }

        private void OnDestroy()
        {
            playerDriver?.Unsubscribe();
        }

        private void OnDrawGizmosSelected()
        {
            DrawShieldBoundaryGizmo();
        }

        private void DrawShieldBoundaryGizmo()
        {
            if (shipClass == null) return;

            // Find shield module config in the ship class
            ShieldModuleConfig shieldConfig = null;
            foreach (var slot in shipClass.MultiSlots)
            {
                if (slot.defaultModule is ShieldModuleConfig config)
                {
                    shieldConfig = config;
                    break;
                }
            }

            if (shieldConfig == null || !shieldConfig.EnableBoundary) return;

            // Draw the ellipse boundary
            Vector2 size = shieldConfig.BoundarySize;
            Vector2 offset = shieldConfig.BoundaryOffset;

            Gizmos.color = new Color(0f, 0.8f, 1f, 0.6f);

            int segments = 32;
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 localPoint = new Vector3(
                    Mathf.Cos(angle) * size.x + offset.x,
                    Mathf.Sin(angle) * size.y + offset.y,
                    0f
                );
                Vector3 worldPoint = transform.TransformPoint(localPoint);

                if (i > 0)
                {
                    Gizmos.DrawLine(prevPoint, worldPoint);
                }
                prevPoint = worldPoint;
            }

            // Draw center cross
            Gizmos.color = Color.cyan;
            Vector3 center = transform.TransformPoint(new Vector3(offset.x, offset.y, 0f));
            float markerSize = 0.15f;
            Gizmos.DrawLine(center - Vector3.right * markerSize, center + Vector3.right * markerSize);
            Gizmos.DrawLine(center - Vector3.up * markerSize, center + Vector3.up * markerSize);
        }
    }
}
