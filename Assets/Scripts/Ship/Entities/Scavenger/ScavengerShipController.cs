using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Starfire
{
    public class ScavengerShipController : StandardAIController, IScavengerData
    {
        private StateMachine stateMachine;
        private Transform scavengerTransform;
        private GameObject scavengerObject;
        private Rigidbody2D scavengerRigid2D;

        // Target info
        // private Dictionary<GameObject, (int, float)> scavengerTargetList = new Dictionary<GameObject, (int, float)>();

        public StateMachine ScavengerStateMachine { get => stateMachine; }
        public Transform ScavengerTransform { get => scavengerTransform; }
        public Rigidbody2D ScavengerRigidbody { get => scavengerRigid2D; }
        public GameObject ScavengerObject { get => scavengerObject; }

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
        }

        protected override void Update()
        {
            base.Update();
            stateMachine.Update();

            if (targetShipTransform == null)
            {
                if (stateMachine.CurrentState is ScavengerIdleState) return;
                stateMachine.ChangeState(new ScavengerIdleState(this));
            }
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

            SetTargetShip(other.transform);
            stateMachine.ChangeState(new ScavengerChaseState(this));

            // if (scavengerTargetList.ContainsKey(other))
            // {
            //     scavengerTargetList[other] = (scavengerTargetList[other].Item1 + 1, Time.time);
            // }
            // else
            // {
            //     scavengerTargetList.Add(other, (1, Time.time));
            // }

            // EvaluateNextTarget();
            UpdateHealthBar(configuration.Health, configuration.MaxHealth);
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
                healthPickupRigidbody.velocity = scavengerRigid2D.velocity;
                healthPickup.GetComponent<Rigidbody2D>().AddForce(randomDirection * explosionForce, ForceMode2D.Impulse);
            }

            base.DestroyShip();
        }

        // public void EvaluateNextTarget()
        // {
        //     List<GameObject> targetObjects = new List<GameObject>(scavengerTargetList.Keys);

        //     foreach (GameObject target in targetObjects)
        //     {
        //         if (target == null)
        //         {
        //             scavengerTargetList.Remove(target);
        //             continue;
        //         }

        //         if (Time.time - scavengerTargetList[target].Item2 < 15f) continue;
                
        //         var distanceToPlayer = Vector2.Distance(target.transform.position, scavengerTransform.position);

        //         if (target.CompareTag("Player") && distanceToPlayer < 180f && Random.Range(0, 100) > 40)
        //         {
        //             targetShipTransform = target.GetComponentInParent<ShipController>().transform;
        //             targetShipRigid2D = targetShipTransform.GetComponent<Rigidbody2D>();
        //             stateMachine.ChangeState(new ScavengerChaseState(this));
        //             return;
        //         }

        //         scavengerTargetList.Remove(target);
                
        //     }

        //     if (scavengerTargetList.Count == 0)
        //     {
        //         stateMachine.ChangeState(new ScavengerIdleState(this));
        //     }
        //     else
        //     {
        //         int maxHits = 0;
        //         GameObject nextTarget = null;

        //         foreach (var target in targetObjects)
        //         {
        //             if (scavengerTargetList.ContainsKey(target) is false) continue;
        //             if (scavengerTargetList[target].Item1 < maxHits) continue;
                    
        //             if (target.CompareTag("Player")) 
        //             {
        //                 if (Random.Range(0, 100) > 20 && targetShipTransform != null) continue;
        //             }

        //             maxHits = scavengerTargetList[target].Item1;
        //             nextTarget = target;
        //         }

        //         if (nextTarget == null) return;

        //         targetShipTransform = nextTarget.GetComponentInParent<ShipController>().transform;
        //         targetShipRigid2D = targetShipTransform.GetComponent<Rigidbody2D>();
        //     }

        //     stateMachine.ChangeState(new ScavengerChaseState(this));
        // }
    }
}
