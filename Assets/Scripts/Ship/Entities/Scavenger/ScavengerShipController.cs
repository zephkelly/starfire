using UnityEngine;

namespace Starfire
{
    public class ScavengerShipController : ShipController
    {
        private StateMachine stateMachine;
        [SerializeField] private float moveSpeed = 160f;

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
            // stateMachine.ChangeState(new ScavengerIdleState(this));
            stateMachine.ChangeState(new ScavengerChaseState(this, transform.GetComponent<Rigidbody2D>(), GameObject.Find("PlayerShip").transform));
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
