using UnityEngine;

namespace Starfire
{
    public class ScavengerShipController : ShipController
    {
        private StateMachine stateMachine;
        private Rigidbody2D scavengerRigid2D;
        [SerializeField] private float moveSpeed = 160f;

        private Transform playerTransform;

        public StateMachine StateMachine { get => stateMachine; }
        public float MoveSpeed { get => moveSpeed; }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            stateMachine = new StateMachine();
            scavengerRigid2D = transform.GetComponent<Rigidbody2D>();
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

            stateMachine.ChangeState(new ScavengerChaseState(this, scavengerRigid2D, playerTransform));
        }

        protected override void Update()
        {
            stateMachine.Update();
            base.Update();
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
    }
}
