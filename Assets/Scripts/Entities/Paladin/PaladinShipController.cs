using UnityEngine;

namespace Starfire
{
    public class PaladinShipController : ShipController
    {

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public override void SetOrbitingBody(CelestialBehaviour orbitingBody, bool isParent = false)
        {
            base.SetOrbitingBody(orbitingBody, isParent);
        }

        public override void DestroyShip()
        {
            int random = Random.Range(2, 4);
            var healthPickupPrefab = Resources.Load<GameObject>("Prefabs/Pickups/PickupHealth");

            for (int i = 0; i < random; i++)
            {
                Vector3 randomDirection = Random.insideUnitCircle;
                float explosionForce = Random.Range(50f, 80f);

                GameObject healthPickup = Instantiate(healthPickupPrefab, transform.position + (randomDirection * 1), Quaternion.identity);
                healthPickup.TryGetComponent(out Rigidbody2D healthPickupRigidbody);
                healthPickupRigidbody.velocity = shipRigidBody.velocity;
                healthPickup.GetComponent<Rigidbody2D>().AddForce(randomDirection * explosionForce, ForceMode2D.Impulse);
            }

            base.DestroyShip();
        }
    }
}
