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
            _shipSystems = new ShipSystems(this, definition.BuildSlotConfigurations());
        }

        public void Initialize(Entity entity, SlotConfiguration[] slotConfigurations)
        {
            Entity = entity;
            _shipSystems = new ShipSystems(this, slotConfigurations);
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
                float maxSpeed = _shipSystems.Propulsion?.Module?.MaxSpeed ?? 10f;
                float speed = maxSpeed * Mathf.Clamp01(throttle);
                Move(moveDirection, speed);
            }
        }

        protected override void ProcessRotation(IControllerDriver driver)
        {
            if (_shipSystems.Rotation == null || !_shipSystems.Rotation.HasModule) return;

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

            _shipSystems.Rotation.Module.ProcessRotation(input, Time.deltaTime);
        }
    }
}
