using UnityEngine;
using Starfire.Entity.Modules.Rotation;

namespace StarfireV2
{
    public class ShipController : MonoBehaviour, IEntityController
    {
        [SerializeField] private ModuleSlotConfiguration[] _slotConfigurations;

        public IEntity Entity => Ship;
        public ShipEntity Ship { get; private set; }

        
        [SerializeField] private EntityControllerDriverStack _driverStack = new();
        public EntityControllerDriverStack DriverStack => _driverStack;

        [SerializeField, Tooltip("Prefab for the gamepad aim reticle. If null, a default one is created at runtime.")]
        private AimReticle aimReticlePrefab;

        public Rigidbody2D Rigid2D { get; private set; }
        public Transform Transform { get; private set; }

        private Camera MainCamera { get; set; }

        private void Awake()
        {
            Rigid2D = GetComponent<Rigidbody2D>();
            Transform = transform;

            // Initialize hardpoint registry before modules so weapons can find their hardpoints
            var hardpointRegistry = GetComponent<V2HardpointRegistry>();
            if (hardpointRegistry != null)
            {
                hardpointRegistry.Initialize();
            }

            Ship = new ShipEntity(EntityType.Ship, GetInstanceID());
            Ship.InitializeModules(this, _slotConfigurations);

            MainCamera = Camera.main;
        }

        private void Start()
        {
            _driverStack.InitializeDrivers();
            InitializeAimReticle();
        }

        private void InitializeAimReticle()
        {
            var playerDriver = _driverStack.Find<PlayerEntityControllerDriver>();
            if (playerDriver == null) return;

            AimReticle reticle;
            if (aimReticlePrefab != null)
            {
                reticle = Instantiate(aimReticlePrefab);
            }
            else
            {
                // Create a default reticle at runtime
                var go = new GameObject("AimReticle");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateDefaultReticleSprite();
                sr.color = new Color(1f, 1f, 1f, 0.7f);
                sr.sortingOrder = 100;
                reticle = go.AddComponent<AimReticle>();
            }

            reticle.SetOwner(Transform);
            playerDriver.SetAimReticle(reticle);
        }

        private static Sprite CreateDefaultReticleSprite()
        {
            // 16x16 circle texture as a simple fallback
            int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            float center = (size - 1) / 2f;
            float radius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    // Ring shape: visible between radius-2 and radius
                    float alpha = (dist > radius - 2f && dist <= radius) ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private void Update()
        {
            var driver = DriverStack.GetActiveDriver();
            if (driver == null || Ship == null) return;

            ProcessWeapons(driver);
            Ship.UpdateModules(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            var driver = DriverStack.GetActiveDriver();
            if (driver == null || Ship == null) return;

            ProcessMovement(driver);
            ProcessRotation(driver);
        }

        // Core
        private void ProcessMovement(IEntityControllerDriver driver)
        {
            var propulsion = Ship.Propulsion;
            if (propulsion == null) return;

            var acceleration = driver.GetDesiredAcceleration();
            if (acceleration.sqrMagnitude > 0.001f)
            {
                // Clamp acceleration to module's max
                float maxAccel = propulsion.Acceleration;
                if (acceleration.magnitude > maxAccel)
                    acceleration = acceleration.normalized * maxAccel;

                ApplyAcceleration(acceleration);
                return;
            }

            var moveDirection = driver.GetMovementDirection();
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                float throttle = driver.GetThrottle();
                float maxSpeed = propulsion.MaxSpeed;
                float accel = propulsion.Acceleration;

                Vector2 targetVelocity = moveDirection.normalized * maxSpeed * Mathf.Clamp01(throttle);
                AccelerateToward(targetVelocity, accel);
            }
        }

        private void AccelerateToward(Vector2 targetVelocity, float acceleration)
        {
            if (Rigid2D == null) return;

            Vector2 currentVel = Rigid2D.linearVelocity;
            Vector2 delta = targetVelocity - currentVel;

            float maxDelta = acceleration * Time.fixedDeltaTime;
            if (delta.magnitude > maxDelta)
                delta = delta.normalized * maxDelta;

            Rigid2D.linearVelocity = currentVel + delta;
        }

        private void ProcessRotation(IEntityControllerDriver driver)
        {
            var rotationModule = Ship.Rotation;
            if (rotationModule == null) return;

            // Skip rotation if driver has no aim target (e.g. AI with no BT output)
            if (!driver.HasAimTarget) return;

            Vector2 aimWorld = ResolveAimWorldPosition(driver);

            var input = new RotationInputData
            {
                KeyboardInput = driver.GetRotationInput(),
                MouseWorldPosition = aimWorld,
                EntityPosition = transform.position,
                CurrentRotation = Rigid2D != null ? Rigid2D.rotation : transform.eulerAngles.z
            };

            rotationModule.ProcessRotation(input, Time.fixedDeltaTime);
        }

        private void ProcessWeapons(IEntityControllerDriver driver)
        {
            Vector2 aimWorld = ResolveAimWorldPosition(driver);

            Vector2 aimDirection = (aimWorld - (Vector2)transform.position).normalized;

            foreach (var weapon in Ship.OffenseModules)
            {
                weapon.SetAimDirection(aimDirection);

                if (driver.IsFirePressed())
                {
                    weapon.Fire();
                }
            }
        }

        private Vector2 ResolveAimWorldPosition(IEntityControllerDriver driver)
        {
            Vector2 raw = driver.GetAimDirection();

            if (driver.IsWorldSpaceAim)
                return raw; // Already a world position (e.g. reticle or AI target)

            return ScreenToWorldPosition(raw);
        }

        // Utils
        private void Move(Vector2 direction, float speed)
        {
            if (Rigid2D != null)
            {
                Vector2 movement = direction.normalized * speed * Time.deltaTime;
                Rigid2D.AddForce(movement, ForceMode2D.Force);
            }
        }


        private void ApplyAcceleration(Vector2 acceleration)
        {
            if (Rigid2D != null)
            {
                // F = m * a
                Rigid2D.AddForce(acceleration * Rigid2D.mass, ForceMode2D.Force);
            }
        }

        private void ApplyRotation(float angle)
        {
            if (Rigid2D != null)
            {
                Rigid2D.rotation = angle;
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        
        private Vector2 ScreenToWorldPosition(Vector2 screenPos)
        {
            if (MainCamera == null) return Vector2.zero;
            float cameraDistance = Mathf.Abs(MainCamera.transform.position.z);
            Vector3 worldPos = MainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cameraDistance));
            return new Vector2(worldPos.x, worldPos.y);
        }
    }
}