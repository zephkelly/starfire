using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Starfire
{
    public abstract class ShipController: MonoBehaviour
    {
        public Ship Ship { get; private set; }
        public AICore AICore { get; set; }
        public StateMachine ShipStateMachine { get; private set; }
        public CelestialBehaviour OrbitingBody { get; private set; }
        public bool IsOrbiting { get; private set; }

        public GameObject ShipObject { get => shipObject; }
        public Transform ShipTransform { get => shipTransform; }
        public Rigidbody2D ShipRigidBody { get => shipRigidBody; }
        public SpriteRenderer ShipSpriteRenderer { get => shipSpriteRenderer; }

        protected GameObject shipObject;
        protected Transform shipTransform;
        protected Rigidbody2D shipRigidBody;
        protected SpriteRenderer shipSpriteRenderer;

        private Vector2 orbitalVelocity;

        [Header("Weapon Settings")]
        [SerializeField] protected float fireRate = 0.20f;
        [SerializeField] private ParticleSystem[] weaponProjectilePS;
        private Queue<ParticleSystem> weaponProjectileQueue = new Queue<ParticleSystem>();

        [Header("Thruster Settings")]
        [SerializeField] private ParticleSystem[] shipThrusterPS;
        [SerializeField] private Light2D[] shipThrusterLight;
        [SerializeField] protected Gradient thrusterNormalGradient;
        [SerializeField] protected Gradient thrusterWarpGradient;

        [Header("Health/Damage Settings")]
        [SerializeField] protected Canvas healthbarCanvas;
        [SerializeField] protected Image healthBarFill;
        [SerializeField] protected Color shipDamageColor;
        private const float invulnerabilityTime = 0.5f;

        public virtual void SetShip(Ship ship, AICore aiCore)
        {
            Ship = ship;
            AICore = aiCore;
        }

        protected virtual void Awake()
        {
            if (TryGetComponent(out GameObject go))
            {
                shipObject = go;
            }

            if (TryGetComponent(out Transform t))
            {
                shipTransform = t;
            }

            if (TryGetComponent(out Rigidbody2D rb))
            {
                shipRigidBody = rb;
            }

            if (TryGetComponent(out SpriteRenderer sr))
            {
                shipSpriteRenderer = sr;
            }

            ShipStateMachine = new StateMachine();
        }

        protected virtual void Start()
        {
            healthbarCanvas.enabled = false;
            ChunkManager.Instance.AddShip(this);

            foreach (var thrusterLight in shipThrusterLight)
            {
                thrusterLight.enabled = false;
            }
        }

        protected virtual void Update()
        {
            UpdateTimers();
            
            AICore.Update();
            ShipStateMachine.Update();
        }

        protected virtual void FixedUpdate()
        {
            if (IsOrbiting is true)
            {
                OrbitCelestialBody();
                return;
            }

            ApplyLinearDrag();
        }

        protected virtual void OnParticleCollision(GameObject other)
        {
            if (other == gameObject) return;
            int damage = other.GetComponentInParent<ShipController>().Ship.Configuration.ProjectileDamage;

            Ship.Configuration.Damage(damage, DamageType.Hull);

            if (invulnerabilityTimer > 0) return;
            invulnerabilityTimer = invulnerabilityTime;

            StartCoroutine(InvulnerabilityFlash());
        }

        public virtual void MoveInDirection(Vector2 direction, float speed, bool boost, float manoeuvreSpeed = 60f) //  float maxSpeed
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

        private const float warpFuelConsumeTime = 1;
        private float warpFuelTimer = 0;
        public virtual void WarpInDirection(Vector2 direction, float moveSpeed, bool boost) //, float maxSpeed
        {
            if (Ship.Configuration.WarpFuel <= 0) 
            {
                MoveInDirection(direction, Ship.Configuration.ThrusterMaxSpeed, boost);
                return;
            }

            float velocityPercentage = shipRigidBody.velocity.magnitude / Ship.Configuration.WarpMaxSpeed;
            float newMoveSpeed = Mathf.Lerp(moveSpeed, Ship.Configuration.WarpMaxSpeed, velocityPercentage);

            if (boost is true)
            {
                shipRigidBody.AddForce(direction * newMoveSpeed, ForceMode2D.Force);
            }
            else
            {
                shipRigidBody.AddForce(direction * newMoveSpeed, ForceMode2D.Force);
            }

            if (IsOrbiting is false)
            {
                SetThrusters(boost, direction, true);
            }
            else
            {
                SetThrusters(boost, direction, true);
            }

            //if the ship is moving faster than the max speed, clamp it
            if (shipRigidBody.velocity.magnitude > Ship.Configuration.WarpMaxSpeed)
            {
                shipRigidBody.velocity = shipRigidBody.velocity.normalized * Ship.Configuration.WarpMaxSpeed;
            }

            if (warpFuelTimer > 0)
            {
                warpFuelTimer -= Time.deltaTime;
            }
            else
            {
                warpFuelTimer = warpFuelConsumeTime;
                Ship.Configuration.UseWarpFuel();
            }
        }

        public virtual void RotateToPosition(Vector2 targetPosition, float degreesPerSecond)
        {
            Vector2 newDirection = new Vector2(targetPosition.x - transform.position.x, targetPosition.y - transform.position.y);
            float targetAngle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg - 90;
            float currentAngle = transform.eulerAngles.z;
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            float rotation = Mathf.Sign(angleDifference) * Mathf.Min(Mathf.Abs(angleDifference), degreesPerSecond * Time.deltaTime);

            transform.Rotate(Vector3.forward, rotation);
        }

        public virtual void RotateToDirection(Vector2 targetDirection, float degreesPerSecond)
        {
            float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg - 90;
            float currentAngle = transform.eulerAngles.z;
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            float rotation = Mathf.Sign(angleDifference) * Mathf.Min(Mathf.Abs(angleDifference), degreesPerSecond * Time.deltaTime);

            transform.Rotate(Vector3.forward, rotation);
        }


        private int currentWeaponIndex = 0;
        protected float currentProjectileFireTimer = 0;
        public virtual void FireProjectileToPosition(Vector2 targetPosition)
        {
            if (currentProjectileFireTimer > 0) return;
            currentProjectileFireTimer = fireRate;

            AimWeapons(targetPosition);

            weaponProjectileQueue.Enqueue(weaponProjectilePS[currentWeaponIndex]);
            ParticleSystem weaponPS = weaponProjectileQueue.Dequeue();

            weaponPS.Play();

            currentWeaponIndex = (currentWeaponIndex + 1) % weaponProjectilePS.Length;
        }

        public virtual void Transport(Vector2 offset)
        {
            // Player
            transform.position += (Vector3)offset;

            // Particles
            Dictionary<ParticleSystem, ParticleSystem.Particle[]> particles = new Dictionary<ParticleSystem, ParticleSystem.Particle[]>();

            // Get Thrusters
            for (int i = 0; i < shipThrusterPS.Length; i++)
            {
                var particleArray = new ParticleSystem.Particle[shipThrusterPS[i].particleCount];
                shipThrusterPS[i].GetParticles(particleArray);

                particles.Add(shipThrusterPS[i], particleArray);
            }

            // Get Lasers
            for (int i = 0; i < weaponProjectilePS.Length; i++)
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

        private const float healthbarDisableTime = 5f;
        private float timeTillDisableHealthbar = 0f;
        protected virtual void UpdateTimers()
        {
            if (currentProjectileFireTimer > 0) currentProjectileFireTimer -= Time.deltaTime;

            if (invulnerabilityTimer > 0) invulnerabilityTimer -= Time.deltaTime;

            if (timeTillDisableHealthbar > 0) 
            {
                timeTillDisableHealthbar -= Time.deltaTime;
            }
            else
            {
                DisableHealthbar();
            }
        }

        public virtual void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthbarCanvas.enabled is false) healthbarCanvas.enabled = true;
            healthBarFill.fillAmount = currentHealth / maxHealth;

            timeTillDisableHealthbar = healthbarDisableTime;
        }

        public virtual void UpdateWarpFuel(float currentWarpFuel, float maxWarpFuel)
        {
            // to be implemented for AI
        }

        public virtual void DisableHealthbar()
        {
            if (healthbarCanvas.enabled is false) return;
            healthbarCanvas.enabled = false;
        }

        public virtual void SetOrbitingBody(CelestialBehaviour _orbitingBody, bool returningToParent = false)
        {
            if (OrbitingBody == _orbitingBody) return;

            if (_orbitingBody == null)
            {
                Debug.LogError("Error: SetOrbitingBody() null reference");
                return;
            }
            
            if (OrbitingBody != null && OrbitingBody.CelestialBodyType is CelestialBodyType.Planet && _orbitingBody.CelestialBodyType is CelestialBodyType.Planet)
            {
                if (_orbitingBody.Mass <= OrbitingBody.Mass) return;
            }

            OrbitingBody = _orbitingBody;
            IsOrbiting = true;
        }

        public virtual void DestroyShip()
        {
            Destroy(gameObject);
        }

        public virtual void RemoveOrbitingBody()
        {
            OrbitingBody = null;
            IsOrbiting = false;
        }

        public void SetThrusters(bool isActive, Vector2 movementDirection, bool isWarping)
        {
            if (movementDirection.magnitude > 0.1f && isActive is true)
            {
                for (int i = 0; i < shipThrusterPS.Length; i++)
                {
                    UpdateThrusterGradientByVelocity(shipThrusterPS[i], isWarping);

                    if (shipThrusterPS[i].isPlaying is true) continue;
                    shipThrusterPS[i].Play();
                    shipThrusterLight[i].enabled = true;
                }
            }
            else
            {
                for (int i = 0; i < shipThrusterPS.Length; i++)
                {
                    if (shipThrusterPS[i].isPlaying is false) continue;
                    shipThrusterPS[i].Stop();
                    shipThrusterLight[i].enabled = false;
                }
            }
        }

        protected void OrbitCelestialBody()
        {
            if (OrbitingBody == null)
            {
                Debug.LogError("Error: No orbiting body. Null reference");
                return;
            }

            if (OrbitingBody.OrbitController == null)
            {
                Debug.LogError("Error: No orbiting controller. Null reference");
                return;
            }

            int desiredOrbitDirection = OrbitingBody.OrbitController.GetOrbitDirection(shipRigidBody);
            Vector2 desiredVelocity = OrbitingBody.OrbitController.GetOrbitalVelocity(shipRigidBody);

            if (OrbitingBody.CelestialBodyType == CelestialBodyType.Star)
            {
                orbitalVelocity = (desiredVelocity * desiredOrbitDirection) + OrbitingBody.OrbitController.GetVelocity();
            }
            else if (OrbitingBody.CelestialBodyType == CelestialBodyType.Planet)
            {
                orbitalVelocity = desiredVelocity + OrbitingBody.OrbitController.GetVelocity();
            }

            ApplyOrbitalDrag();

            MaintainRelativeOrbitPosition();
        }

        private void ApplyOrbitalDrag()
        {
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
        }

        private void MaintainRelativeOrbitPosition()
        {
            Vector2 relativePosition = new Vector2(
                transform.position.x - OrbitingBody.WorldPosition.x,
                transform.position.y - OrbitingBody.WorldPosition.y
            );

            transform.position = OrbitingBody.WorldPosition + relativePosition;
        }

        protected void ApplyLinearDrag()
        {
            float currentSpeed = shipRigidBody.velocity.magnitude;
            float thrusterSpeed = 160f;

            if (currentSpeed > thrusterSpeed)
            {
                shipRigidBody.AddForce((-shipRigidBody.velocity * 0.8f) * shipRigidBody.mass, ForceMode2D.Force);
            }
            else
            {
                shipRigidBody.AddForce(-shipRigidBody.velocity * shipRigidBody.mass, ForceMode2D.Force);
            }

            if (shipRigidBody.velocity.magnitude < 0.1f) shipRigidBody.velocity = Vector2.zero;
        }

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

        private void UpdateThrusterGradientByVelocity(ParticleSystem thruster, bool isWarping)
        {
            if (isWarping is true)
            {
                var colorOverLifetime = thruster.colorOverLifetime;
                colorOverLifetime.color = thrusterWarpGradient;
            }
            else
            {
                var colorOverLifetime = thruster.colorOverLifetime;
                colorOverLifetime.color = thrusterNormalGradient;
            }
        }

        protected float invulnerabilityTimer = 0f;
        private IEnumerator InvulnerabilityFlash()
        {
            shipSpriteRenderer.color = shipDamageColor;
            yield return new WaitForSeconds(0.4f);
            shipSpriteRenderer.color = Color.white;
        }

        private void OnDestroy()
        {
            ChunkManager.Instance.RemoveShip(this);
        }
    }
}