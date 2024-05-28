using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace Starfire
{
    public interface IShipController
    {
        ShipConfiguration Configuration { get; }
        ShipInventory Inventory { get; }
        CelestialBehaviour OrbitingBody { get; }
        bool IsOrbiting { get; }
        void SetOrbitingBody(CelestialBehaviour orbitingBody, bool isParent = false);
        void RemoveOrbitingBody();
        int Damage(int damage, DamageType damageType);
        void Repair(int repair, DamageType damageType);
        void Move(Vector2 direction, float speed, bool boost, float manoeuvreSpeed);
        void Transport(Vector2 position);
        void Rotate(Vector2 direction, float speed, float lerpSpeed);
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class ShipController : MonoBehaviour, IShipController
    {
        protected ShipConfiguration configuration;
        protected ShipInventory inventory;
        protected Rigidbody2D shipRigidBody;
        protected ParticleSystem shipThrusterPS;
        protected Light2D shipThrusterLight;
        
        protected CelestialBehaviour orbitingBody;
        protected Vector2 orbitalVelocity;
        protected Vector2 lastOrbitalVelocity;
        protected bool isOrbiting = false;

        public ShipConfiguration Configuration => configuration;
        public ShipInventory Inventory => inventory;
        public CelestialBehaviour OrbitingBody => orbitingBody;
        public bool IsOrbiting => isOrbiting;

        protected virtual void Awake()
        {
            configuration = ScriptableObject.CreateInstance("ShipConfiguration") as ShipConfiguration;

            shipRigidBody = GetComponent<Rigidbody2D>();
            shipThrusterPS = GetComponentInChildren<ParticleSystem>();
            shipThrusterLight = GetComponentInChildren<Light2D>();

            shipThrusterLight.enabled = false;
        }

        protected virtual void Start()
        {
            shipRigidBody.centerOfMass = Vector2.zero;
        }

        protected virtual void Update() {}

        protected virtual void FixedUpdate()
        { 
            if (isOrbiting)
            {
                // OrbitCelestialBody();
                return;
            }

            // ApplyLinearDrag();
        }
        
        public virtual void SetOrbitingBody(CelestialBehaviour _orbitingBody, bool isParent = false)
        {
            if (_orbitingBody == null)
            {
                Debug.LogError("Error: SetOrbitingBody() null reference");
                return;
            }

            if (orbitingBody is not null && orbitingBody.CelestialBodyType != CelestialBodyType.Star && _orbitingBody.Mass <= orbitingBody.Mass)
            {
                Debug.LogError("Error: Current Orbiting body is larger than new orbiting body");
                return;
            }

            orbitingBody = _orbitingBody;
            isOrbiting = true;
        }

        public virtual void RemoveOrbitingBody()
        {
            orbitingBody = null;
            isOrbiting = false;
        }

        protected void OrbitCelestialBody()
        {
            if (orbitingBody == null)
            {
                Debug.LogError("Error: No orbiting body. Null reference");
                return;
            }

            if (orbitingBody.OrbitController == null)
            {
                Debug.LogError("Error: No orbiting controller. Null reference");
                return;
            }

            //Set constant orbit velocity
            lastOrbitalVelocity = orbitalVelocity;

            orbitalVelocity = orbitingBody.OrbitController.GetOrbitalVelocity(shipRigidBody);

            //   orbitalVelocity = Vector2.Lerp(orbitalVelocity, desiredOrbitalVelocity, Time.fixedDeltaTime * 1.0f);

            shipRigidBody.velocity -= lastOrbitalVelocity;   //Working around unity physics
            shipRigidBody.velocity += orbitalVelocity;

            Vector2 orbitalDragX = new Vector2(orbitalVelocity.x - shipRigidBody.velocity.x, 0);
            Vector2 orbitalDragY = new Vector2(0, orbitalVelocity.y - shipRigidBody.velocity.y);

            //Orbital drag
            if (shipRigidBody.velocity.x != orbitalVelocity.x) 
            {
                shipRigidBody.AddForce(orbitalDragX * shipRigidBody.mass, ForceMode2D.Force);
            }

            if (shipRigidBody.velocity.y != orbitalVelocity.y) 
            {
                shipRigidBody.AddForce(orbitalDragY * shipRigidBody.mass, ForceMode2D.Force);
            } 

            Vector2 relativePosition = (Vector2)transform.position - orbitingBody.WorldPosition;
            transform.position = orbitingBody.WorldPosition + relativePosition;
        }

        public virtual void Move(Vector2 direction, float speed, bool boost, float manoeuvreSpeed = 60f) //TODO: Add double tap to boost
        {
            if (boost is true)
            {
                shipRigidBody.AddForce(direction * speed, ForceMode2D.Force);
            }
            else 
            {
                shipRigidBody.AddForce(direction * manoeuvreSpeed, ForceMode2D.Force);
            }

            if (direction.magnitude > 0.1f && boost is true)
            {
                shipThrusterPS.Play();
                shipThrusterLight.enabled = true;
            }
            else
            {
                shipThrusterPS.Stop();
                shipThrusterLight.enabled = false;
            }
        }

        public virtual void Transport(Vector2 offset)
        {
            transform.position += (Vector3)offset;

            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[shipThrusterPS.particleCount];
            shipThrusterPS.GetParticles(particles);

            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].position += (Vector3)offset;
            }

            shipThrusterPS.SetParticles(particles);
        }

        protected void ApplyLinearDrag()
        {
            shipRigidBody.AddForce(-shipRigidBody.velocity * shipRigidBody.mass, ForceMode2D.Force);
            
            if (shipRigidBody.velocity.magnitude < 0.1f) shipRigidBody.velocity = Vector2.zero;
        }

        public void Rotate(Vector2 direction, float speed, float lerpSpeed = 0f)
        {
            throw new System.NotImplementedException();
        }

        public int Damage(int damage, DamageType damageType) => configuration.Damage(damage, damageType);

        public void Repair(int repair, DamageType damageType) => configuration.Repair(repair, damageType);
    }
}