using UnityEngine;

namespace Starfire
{
    public class PaladinShipController : ShipController
    {
        public StateMachine StateMachine { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            StateMachine = new StateMachine();
        }

        protected override void Start()
        {
            var newConfiguration = ScriptableObject.CreateInstance("ShipConfiguration") as ShipConfiguration;
            newConfiguration.SetConfiguration(this, 360, 100, 100, 100, 160, 1500, 200, 6);
            var newTransponder = new Transponder("Paladin", Faction.Friendly, 90000);
            var newInventory = new Inventory();

            var newShip = new Ship(newConfiguration, newTransponder, newInventory);
            SetShip(newShip, new StandardAICore());

            base.Start();  
        }

        protected override void Update()
        {
            base.Update();
            ShipCore.Update();

            if (ShipCore.CurrentTarget == null)
            {
                StateMachine.ChangeState(new PaladinIdleState(this));
            }

            StateMachine.Update();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
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