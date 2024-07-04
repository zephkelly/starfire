using UnityEngine;

namespace Starfire
{
    public class PaladinShipController : ShipController
    {
        

        public override void SetShip(Ship ship, ShipCore shipCore)
        {
            base.SetShip(ship, shipCore);
        }

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

            if (ShipCore == null)
            {
                return;
            }

            ShipCore.Update();

            if (ShipCore.CurrentTarget == null)
            {
                StateMachine.ChangeState(new PaladinIdleState(this));
            }
            else
            {
                StateMachine.ChangeState(new PaladinChaseState(this));
            }

            StateMachine.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (ShipCore == null)
            {
                return;
            }

            ShipCore.Update();

            if (ShipCore.CurrentTarget == null)
            {
                StateMachine.ChangeState(new PaladinIdleState(this));
            }

            StateMachine.FixedUpdate();
        }

        public override void SetOrbitingBody(CelestialBehaviour orbitingBody, bool isParent = false)
        {
            base.SetOrbitingBody(orbitingBody, isParent);
        }

        protected override void OnParticleCollision(GameObject other)
        {
            base.OnParticleCollision(other);

            var otherShipController = other.GetComponentInParent<ShipController>();
            var otherShipGameObject = otherShipController.gameObject;

            if (ShipCore.SetTarget(otherShipGameObject))
            {
                StateMachine.ChangeState(new PaladinChaseState(this));
            }

            UpdateHealth(Ship.Configuration.Health, Ship.Configuration.MaxHealth);
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
