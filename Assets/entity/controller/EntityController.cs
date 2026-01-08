using UnityEngine;
using Starfire.Entity.Modules;
using Starfire.Entity.Modules.Rotation;

namespace Starfire.Entity
{
    public class EntityController : MonoBehaviour
    {
        public Entity Entity { get; private set; }
        public Rigidbody2D Rigidbody { get; private set; }
        public ControllerDriverStack DriverStack { get; } = new();
        public ModuleSlot<IRotationModule> RotationSlot { get; private set; }

        private Camera _mainCamera;

        private void Awake()
        {
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                Rigidbody = rb;
            }

            _mainCamera = Camera.main;
            RotationSlot = new ModuleSlot<IRotationModule>(this);
        }

        public void Initialize(Entity entity)
        {
            Entity = entity;
        }

        private void Update()
        {
            var driver = DriverStack.GetActiveDriver();
            if (driver == null || Entity == null) return;

            var moveDirection = driver.GetMovementDirection();

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Move(moveDirection, Entity.MoveSpeed);
            }

            ProcessRotation(driver);
        }

        private void ProcessRotation(IControllerDriver driver)
        {
            if (!RotationSlot.HasModule) return;

            Vector2 mouseWorld = Vector2.zero;
            if (_mainCamera != null)
            {
                Vector2 aimScreenPos = driver.GetAimDirection();
                Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(aimScreenPos.x, aimScreenPos.y, 0f));
                mouseWorld = new Vector2(worldPos.x, worldPos.y);
            }

            var input = new RotationInputData
            {
                KeyboardInput = driver.GetRotationInput(),
                MouseWorldPosition = mouseWorld,
                EntityPosition = transform.position,
                CurrentRotation = Rigidbody != null ? Rigidbody.rotation : transform.eulerAngles.z
            };

            RotationSlot.Module.ProcessRotation(input, Time.deltaTime);
        }

        public void Move(Vector2 direction, float speed)
        {
            if (Rigidbody != null)
            {
                Vector2 movement = direction.normalized * speed * Time.deltaTime;
                Rigidbody.AddForce(movement, ForceMode2D.Force);
            }
        }

        public void SetRotationModule(RotationModuleConfig config)
        {
            if (config != null)
            {
                RotationSlot.Equip(config.CreateModule());
            }
            else
            {
                RotationSlot.Unequip();
            }
        }

        public void SetRotationModule(IRotationModule module)
        {
            RotationSlot.Equip(module);
        }
    }
}