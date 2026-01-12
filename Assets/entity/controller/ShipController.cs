using UnityEngine;
using Starfire.Entity.Modules;
using Starfire.Entity.Modules.Rotation;

namespace Starfire.Entity
{
    public class ShipController : EntityControllerBase
    {
        private ShipSystems _shipSystems;

        public override IEntitySystems Systems => _shipSystems;
        public ShipSystems ShipSystems => _shipSystems;

        public void Initialize(ShipClassDefinition definition)
        {
            Entity = definition.CreateShip();
            _shipSystems = new ShipSystems(this, definition.BuildMultiSlotConfigurations());
        }

        public void Initialize(Entity entity, MultiSlotConfiguration[] configurations)
        {
            Entity = entity;
            _shipSystems = new ShipSystems(this, configurations);
        }

        protected override void Update()
        {
            var driver = DriverStack.GetActiveDriver();
            if (driver == null || Entity == null || Systems == null) return;

            ProcessRotation(driver);
            ProcessWeapons(driver);
            Systems.UpdateAll(Time.deltaTime);
        }

        protected override void ProcessMovement(IControllerDriver driver)
        {
            // Check for physics-based acceleration mode first (AI steering)
            var acceleration = driver.GetDesiredAcceleration();
            if (acceleration.sqrMagnitude > 0.001f)
            {
                ApplyAcceleration(acceleration);
                return;
            }

            // Standard direction/throttle mode (player input)
            var moveDirection = driver.GetMovementDirection();
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                float throttle = driver.GetThrottle();
                float maxSpeed = _shipSystems.PrimaryImpulse?.MaxSpeed ?? 10f;
                float speed = maxSpeed * Mathf.Clamp01(throttle);
                Move(moveDirection, speed);
            }
        }

        protected override void ProcessRotation(IControllerDriver driver)
        {
            var rotationModule = _shipSystems.PrimaryRotation;
            if (rotationModule == null) return;

            Vector2 aimWorld = driver.IsWorldSpaceAim
                ? driver.GetAimDirection()
                : ScreenToWorldPosition(driver.GetAimDirection());

            var input = new RotationInputData
            {
                KeyboardInput = driver.GetRotationInput(),
                MouseWorldPosition = aimWorld,
                EntityPosition = transform.position,
                CurrentRotation = Rigidbody != null ? Rigidbody.rotation : transform.eulerAngles.z
            };

            rotationModule.ProcessRotation(input, Time.deltaTime);
        }

        protected virtual void ProcessWeapons(IControllerDriver driver)
        {
            // Calculate aim direction in world space
            Vector2 aimWorld = driver.IsWorldSpaceAim
                ? driver.GetAimDirection()
                : ScreenToWorldPosition(driver.GetAimDirection());

            Vector2 aimDirection = (aimWorld - (Vector2)transform.position).normalized;

            // Update all weapons with aim direction and fire if requested
            foreach (var weapon in _shipSystems.AllWeapons)
            {
                weapon.SetAimDirection(aimDirection);

                if (driver.IsFirePressed())
                {
                    weapon.Fire();
                }
            }
        }
    }
}
