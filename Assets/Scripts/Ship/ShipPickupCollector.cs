using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Starfire
{
    public class ShipPickupCollector : MonoBehaviour
    {
        [SerializeField] private ShipController shipController;
        private const float pickupSpeed = 0.95f;

        private void Awake()
        {
            if (shipController == null)
            {
                Debug.LogWarning("Error: Ship controller not found!");
            }
        }

        private async void OnTriggerEnter2D(Collider2D otherCollider)
        {
            if (!otherCollider.CompareTag("Pickup")) return;

            otherCollider.isTrigger = true;

            //Wait for the pickup to get in range
            Task lerpTask = PickupLerp(otherCollider.transform);
            await Task.WhenAll(lerpTask);

            if (otherCollider == null) return;
            if (otherCollider.TryGetComponent(out HealthPickup pickup))
            {
                switch(pickup.Type)
                {
                    case PickupType.Health:
                        shipController.Ship.Configuration.Repair(pickup.Value, DamageType.Hull);
                        shipController.UpdateHealth(shipController.Ship.Configuration.Health, shipController.Ship.Configuration.MaxHealth);
                        break;
                    default:
                        Debug.LogWarning("Error: Pickup type not found");
                        break;
                }

                Destroy(otherCollider.gameObject);
            }
        }

        private async Task PickupLerp(Transform pickupTransform)
        {
            Vector2 directionToPlayer;
            float distanceToPlayer;
            float lerpTime = 0;

            do
            {
                if (pickupTransform == null) break;

                directionToPlayer = transform.position - pickupTransform.position;
                directionToPlayer.Normalize();

                distanceToPlayer = Vector2.Distance(transform.position, pickupTransform.position);
                float force = 2 / distanceToPlayer;

                //lerp from our position to player position by time and force
                pickupTransform.position = Vector2.Lerp(
                    pickupTransform.position, 
                    transform.position, 
                    lerpTime * force * pickupSpeed
                );

                lerpTime += Time.deltaTime;
                await Task.Yield();
            }
            while (distanceToPlayer > 0.1f);
        }
    }
}