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
        void MoveInDirection(Vector2 direction, float speed, float maxSpeed, bool boost, float manoeuvreSpeed);
        void WarpInDirection(Vector2 direction, float moveSpeed, float maxSpeed, bool boost);
        void FireProjectile();
        void Transport(Vector2 position);
        void RotateToVector(Vector2 direction, float speed);
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class ShipController : MonoBehaviour, IShipController
    {
        protected ShipConfiguration configuration;
        protected ShipInventory inventory;
        protected Rigidbody2D shipRigidBody;
        protected SpriteRenderer shipSprite;
        [SerializeField] protected List<ParticleSystem> shipThrusterPS;
        [SerializeField] protected List<Light2D> shipThrusterLight;

        protected CelestialBehaviour orbitingBody;
        protected Vector2 orbitalVelocity;
        protected Vector2 lastOrbitalVelocity;
        protected bool isOrbiting = false;
        protected const float invulnerabilityTime = 0.5f;

        [Header("Weapons")]
        [SerializeField] protected List<ParticleSystem> weaponProjectilePS = new List<ParticleSystem>();
        protected Queue<ParticleSystem> weaponProjectileQueue = new Queue<ParticleSystem>();
        [SerializeField] protected float fireRate = 0.20f;
        protected float currentProjectileFireTimer = 0;

        [Header("Configuration")]
        [SerializeField] protected Color shipDamageColor;
        [SerializeField] protected Gradient thrusterNormalGradient;
        [SerializeField] protected Gradient thrusterWarpGradient;

        public ShipConfiguration Configuration => configuration;
        public ShipInventory Inventory => inventory;
        public CelestialBehaviour OrbitingBody => orbitingBody;
        public bool IsOrbiting => isOrbiting;

        public void Damage(int damage, DamageType damageType) => configuration.Damage(damage, damageType);
        public void Repair(int repair, DamageType damageType) => configuration.Repair(repair, damageType);

        protected virtual void Awake()
        {
            configuration = ScriptableObject.CreateInstance("ShipConfiguration") as ShipConfiguration;
            configuration.SetConfiguration(this, 100, 100, 100, 100);

            shipRigidBody = GetComponent<Rigidbody2D>();
            shipSprite = GetComponent<SpriteRenderer>();

            foreach (var thrusterLight in shipThrusterLight)
            {
                thrusterLight.enabled = false;
            }
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

        public virtual void MoveInDirection(Vector2 direction, float speed, float maxSpeed, bool boost, float manoeuvreSpeed = 60f) //TODO: Add double tap to boost
        {
            if (boost is true)
            {
                shipRigidBody.AddForce(direction * speed, ForceMode2D.Force);
            }
            else 
            {
                shipRigidBody.AddForce(direction * manoeuvreSpeed, ForceMode2D.Force);
            }

            SetThrusters(boost, direction, false);
        }

        public virtual void WarpInDirection(Vector2 direction, float moveSpeed, float maxSpeed, bool boost)
        {
            if (boost is true)
            {
                shipRigidBody.AddForce(direction * moveSpeed, ForceMode2D.Force);
            }
            else
            {
                shipRigidBody.AddForce(direction * moveSpeed, ForceMode2D.Force);
            }

            if (isOrbiting is false)
            {
                SetThrusters(boost, direction, true);
            }
            else
            {
                SetThrusters(boost, direction, false);
            }

            //if the ship is moving faster than the max speed, clamp it
            if (shipRigidBody.velocity.magnitude > maxSpeed)
            {
                shipRigidBody.velocity = shipRigidBody.velocity.normalized * maxSpeed;
            }
        }
        
        private void SetThrusters(bool isActive, Vector2 movementDirection, bool isWarping)
        {
            if (movementDirection.magnitude > 0.1f && isActive is true)
            {
                for (int i = 0; i < shipThrusterPS.Count; i++)
                {
                    UpdateThrusterGradientByVelocity(shipThrusterPS[i], isWarping);

                    if (shipThrusterPS[i].isPlaying is true) continue;
                    shipThrusterPS[i].Play();
                    shipThrusterLight[i].enabled = true;
                }
            }
            else
            {
                for (int i = 0; i < shipThrusterPS.Count; i++)
                {
                    if (shipThrusterPS[i].isPlaying is false) continue;
                    shipThrusterPS[i].Stop();
                    shipThrusterLight[i].enabled = false;
                }
            }
        }

        private void UpdateThrusterGradientByVelocity(ParticleSystem thruster, bool isWarping)
        {
            if (isWarping is true)
            {
                var main = thruster.main;
                var colorOverLifetime = thruster.colorOverLifetime;
                var sizeOverLifetime = thruster.sizeOverLifetime;

                colorOverLifetime.color = thrusterWarpGradient;
            }
            else
            {
                var main = thruster.main;
                var colorOverLifetime = thruster.colorOverLifetime;
                var sizeOverLifetime = thruster.sizeOverLifetime;

                colorOverLifetime.color = thrusterNormalGradient;
            }
        }   
        
        public virtual void RotateToVector(Vector2 targetPosition, float degreesPerSecond)
        {
            Vector2 newDirection = new Vector2(targetPosition.x - transform.position.x, targetPosition.y - transform.position.y);
            float targetAngle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg - 90;
            float currentAngle = transform.eulerAngles.z;
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            float rotation = Mathf.Sign(angleDifference) * Mathf.Min(Mathf.Abs(angleDifference), degreesPerSecond * Time.deltaTime);

            transform.Rotate(Vector3.forward, rotation);
        }

        private int currentWeaponIndex = 0;
        public virtual void FireProjectile()
        {
            if (currentProjectileFireTimer > 0) return;
            currentProjectileFireTimer = fireRate;

            weaponProjectileQueue.Enqueue(weaponProjectilePS[currentWeaponIndex]);
            ParticleSystem weaponPS = weaponProjectileQueue.Dequeue();

            weaponPS.Play();

            currentWeaponIndex = (currentWeaponIndex + 1) % weaponProjectilePS.Count;
        }

        private void UpdateFireRate()
        {
            if (currentProjectileFireTimer <= 0) return;
            currentProjectileFireTimer -= Time.deltaTime;
        }

        // protected virtual void AimWeapons(Quaternion aimDirection)
        // {
        //     foreach (var weapon in weaponProjectilePS)
        //     {
        //         weapon.transform.rotation = aimDirection;
        //     }
        // }

        protected virtual void AimWeapons(Vector2 targetPosition)
        {
            foreach (var weapon in weaponProjectilePS)
            {
                Vector2 direction = targetPosition - (Vector2)weapon.transform.position;
                
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.Euler(0, 0, angle - 90);

                weapon.transform.rotation = rotation;
            }
        }

        public virtual void Transport(Vector2 offset)
        {
            // Player
            transform.position += (Vector3)offset;

            // Particles
            Dictionary<ParticleSystem, ParticleSystem.Particle[]> particles = new Dictionary<ParticleSystem, ParticleSystem.Particle[]>();

            // Get Thrusters
            for (int i = 0; i < shipThrusterPS.Count; i++)
            {
                var particleArray = new ParticleSystem.Particle[shipThrusterPS[i].particleCount];
                shipThrusterPS[i].GetParticles(particleArray);

                particles.Add(shipThrusterPS[i], particleArray);
            }

            // Get Lasers
            for (int i = 0; i < weaponProjectilePS.Count; i++)
            {
                var particleArray = new ParticleSystem.Particle[weaponProjectilePS[i].particleCount];
                weaponProjectilePS[i].GetParticles(particleArray);

                particles.Add(weaponProjectilePS[i], particleArray);
            }

            // Update Particle Positions
            foreach (var particle in particles)
            {
                for (int i = 0; i < particle.Value.Length; i++)
                {
                    Vector3 newPosition = particle.Value[i].position + (Vector3)offset;
                    particle.Value[i].position = newPosition;
                    particle.Value[i].startLifetime -= 0.008f;
                }

                particle.Key.SetParticles(particle.Value);
            }
        }

        protected void ApplyLinearDrag()
        {
            shipRigidBody.AddForce(-shipRigidBody.velocity * shipRigidBody.mass, ForceMode2D.Force);
            
            if (shipRigidBody.velocity.magnitude < 0.1f) shipRigidBody.velocity = Vector2.zero;
        }

        private void OnDestroy()
        {
            ChunkManager.Instance.RemoveShip(this);
        }

        protected virtual void OnParticleCollision(GameObject other)
        {
            if (other == gameObject) return;
            int damage = other.GetComponentInParent<ShipController>().Configuration.ProjectileDamage;

            Damage(damage, DamageType.Hull);

            if (invulnerabilityTimer > 0) return;
            invulnerabilityTimer = invulnerabilityTime;

            StartCoroutine(InvulnerabilityFlash());
        }

        private float invulnerabilityTimer = 0f;
        private IEnumerator InvulnerabilityFlash()
        {
            shipSprite.color = shipDamageColor;
            yield return new WaitForSeconds(0.4f);
            shipSprite.color = Color.white;
        }
    }
}