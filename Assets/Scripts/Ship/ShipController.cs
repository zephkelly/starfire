using System.Collections;
using System.Collections.Generic;
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
        void Damage(int damage, DamageType damageType);
        void Repair(int repair, DamageType damageType);
        void Move(Vector2 direction, float speed, bool boost, float manoeuvreSpeed);
        void FireProjectile();
        void Transport(Vector2 position);
        void Rotate(Vector2 direction, float speed, float lerpSpeed);
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class ShipController : MonoBehaviour, IShipController
    {
        protected ShipConfiguration configuration;
        protected ShipInventory inventory;
        protected Rigidbody2D shipRigidBody;
        protected SpriteRenderer shipSprite;
        protected ParticleSystem shipThrusterPS;
        protected Light2D shipThrusterLight;

        protected CelestialBehaviour orbitingBody;
        protected Vector2 orbitalVelocity;
        protected Vector2 lastOrbitalVelocity;
        protected bool isOrbiting = false;
        protected const float invulnerabilityTime = 0.5f;

        [Header("Weapons")]
        [SerializeField] protected List<ParticleSystem> weaponProjectilePS = new List<ParticleSystem>();
        protected Queue<ParticleSystem> weaponProjectileQueue = new Queue<ParticleSystem>();
        [SerializeField] protected float fireRate = 0.20f;
        // protected bool isStandardFire = true;
        protected float currentFireTimer = 0;


        public ShipConfiguration Configuration => configuration;
        public ShipInventory Inventory => inventory;
        public CelestialBehaviour OrbitingBody => orbitingBody;
        public bool IsOrbiting => isOrbiting;

        protected virtual void Awake()
        {
            configuration = ScriptableObject.CreateInstance("ShipConfiguration") as ShipConfiguration;
            configuration.SetConfiguration(this, 100, 100, 100, 100);

            shipRigidBody = GetComponent<Rigidbody2D>();
            shipSprite = GetComponent<SpriteRenderer>();
            shipThrusterPS = GetComponentInChildren<ParticleSystem>();
            shipThrusterLight = GetComponentInChildren<Light2D>();

            shipThrusterLight.enabled = false;
        }

        protected virtual void Start()
        {
            shipRigidBody.centerOfMass = Vector2.zero;

            ChunkManager.Instance.AddShip(this);
        }

        protected virtual void Update() 
        {
            UpdateFireRate();

            if (invulnerabilityTimer > 0) invulnerabilityTimer -= Time.deltaTime;
        }

        protected virtual void FixedUpdate()
        { 
            if (isOrbiting)
            {
                OrbitCelestialBody();
                return;
            }

            ApplyLinearDrag();
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

            Vector2 desiredVelocity = orbitingBody.OrbitController.GetOrbitalVelocity(shipRigidBody);

            //add a smooth lerp, with the current velocity being the start and orbitalVecloty being the end
            // FIX THIS !!! Time.deltaTime * 2000f???
            orbitalVelocity = Vector2.Lerp(shipRigidBody.velocity, desiredVelocity, Time.deltaTime * 2000f);

            shipRigidBody.velocity -= lastOrbitalVelocity;   //Working around unity physics
            shipRigidBody.velocity += orbitalVelocity;

            Vector2 orbitalDragX = new Vector2(orbitalVelocity.x - shipRigidBody.velocity.x, 0);
            Vector2 orbitalDragY = new Vector2(0, orbitalVelocity.y - shipRigidBody.velocity.y);

            //Orbital drag
            if (shipRigidBody.velocity.x > orbitalVelocity.x || shipRigidBody.velocity.x < orbitalVelocity.x)
            {
                shipRigidBody.AddForce(orbitalDragX * shipRigidBody.mass, ForceMode2D.Force);
            }

            if (shipRigidBody.velocity.y > orbitalVelocity.y || shipRigidBody.velocity.y < orbitalVelocity.y)
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

        // private Quaternion GetWeaponRotation(Transform weaponTransform)
        // {
        //     Vector2 direction = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector2)weaponTransform.position;
        //     float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //     float x = -90;
        //     float y = 0;
        //     float z = angle - 90;
        //     return Quaternion.Euler(x, y, z);
        // }

        private int currentWeaponIndex = 0;
        public virtual void FireProjectile()
        {
            if (currentFireTimer > 0) return;
            currentFireTimer = fireRate;

            weaponProjectileQueue.Enqueue(weaponProjectilePS[currentWeaponIndex]);
            ParticleSystem weaponPS = weaponProjectileQueue.Dequeue();

            // weaponPS.transform.localRotation = GetWeaponRotation(weaponPS.transform);
            weaponPS.Play();

            currentWeaponIndex = (currentWeaponIndex + 1) % weaponProjectilePS.Count;
        }

        private void UpdateFireRate()
        {
            if (currentFireTimer <= 0) return;
            currentFireTimer -= Time.deltaTime;
        }

        protected virtual void AimWeapons(Quaternion aimDirection)
        {
            foreach (var weapon in weaponProjectilePS)
            {
                weapon.transform.rotation = aimDirection;
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

        public void Damage(int damage, DamageType damageType) => configuration.Damage(damage, damageType);

        public void Repair(int repair, DamageType damageType) => configuration.Repair(repair, damageType);

        private void OnDestroy()
        {
            ChunkManager.Instance.RemoveShip(this);
        }

        protected virtual void OnParticleCollision(GameObject other)
        {
            int damage = other.GetComponentInParent<ShipController>().Configuration.ProjectileDamage;

            Damage(damage, DamageType.Hull);

            if (invulnerabilityTimer > 0) return;
            invulnerabilityTimer = invulnerabilityTime;

            StartCoroutine(InvulnerabilityFlash());
        }

        private float invulnerabilityTimer = 0f;
        private IEnumerator InvulnerabilityFlash()
        {
            shipSprite.color = Color.red;
            yield return new WaitForSeconds(0.4f);
            shipSprite.color = Color.white;
        }
    }
}