using UnityEngine;

namespace Starfire
{
    public class ScavengerShipController : ShipController, IScavengerData
    {
        private StateMachine stateMachine;
        private Transform scavengerTransform;
        private GameObject scavengerObject;
        private Rigidbody2D scavengerRigid2D;
        private Transform playerTransform;
        private Rigidbody2D playerRigid2D;

        public StateMachine ScavengerStateMachine { get => stateMachine; }
        public Transform ScavengerTransform { get => scavengerTransform; }
        public Rigidbody2D ScavengerRigidbody { get => scavengerRigid2D; }
        public GameObject ScavengerObject { get => scavengerObject; }
        public Transform PlayerTransform { get => playerTransform; }
        public Rigidbody2D PlayerRigidbody { get => playerRigid2D; }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            stateMachine = new StateMachine();
            scavengerTransform = transform;
            scavengerObject = gameObject;
            scavengerRigid2D = transform.GetComponent<Rigidbody2D>();
            
            stateMachine.ChangeState(new ScavengerChaseState(this));
        }

        protected override void Update()
        {
            base.Update();
            stateMachine.Update();
        }

        protected override void FixedUpdate()
        {
            stateMachine.FixedUpdate();
            base.FixedUpdate();
        }

        public override void SetOrbitingBody(CelestialBehaviour orbitingBody, bool isParent = false)
        {
            base.SetOrbitingBody(orbitingBody, isParent);
        }

        public override void RemoveOrbitingBody()
        {
            base.RemoveOrbitingBody();
        }

        protected override void OnParticleCollision(GameObject other)
        {
            base.OnParticleCollision(other);

            if (other.CompareTag("Player"))
            {
                playerTransform = other.GetComponentInParent<ShipController>().transform;
                playerRigid2D = playerTransform.GetComponent<Rigidbody2D>();
                
                if (stateMachine.CurrentState is not ScavengerChaseState)
                {
                    stateMachine.ChangeState(new ScavengerChaseState(this));
                }
            }

            UpdateHealthBar(configuration.Health, configuration.MaxHealth);
        }

        public override void DestroyShip()
        {
            int random = Random.Range(2, 4);
            var healthPickupPrefab = Resources.Load<GameObject>("Prefabs/Pickups/PickupHealth");

            for (int i = 0; i < random; i++)
            {
                var healthPickup = Instantiate(healthPickupPrefab, transform.position, Quaternion.identity);
                healthPickup.GetComponent<Rigidbody2D>().AddForce(Random.insideUnitCircle * 50, ForceMode2D.Impulse);
            }

            base.DestroyShip();
        }
    }
}
