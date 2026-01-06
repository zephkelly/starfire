using UnityEngine;

namespace Starfire.Entity
{
    public class EntityController : MonoBehaviour
    {
        public Entity Entity { get; private set; }
        public Rigidbody2D Rigidbody { get; private set; }
        public ControllerDriverStack DriverStack { get; } = new();

        private void Awake()
        {
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                Rigidbody = rb;
            }
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
            var rotationInput = driver.GetRotationInput();

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Move(moveDirection, Entity.MoveSpeed);
            }

            if (Mathf.Abs(rotationInput) > 0.01f)
            {
                Rotate(rotationInput * Entity.RotationSpeed * Time.deltaTime);
            }
        }

        public void Move(Vector2 direction, float speed)
        {
            if (Rigidbody != null)
            {
                Vector2 movement = direction.normalized * speed * Time.deltaTime;
                Rigidbody.AddForce(movement, ForceMode2D.Force);
            }
        }

        public void Rotate(float angle)
        {
            if (Rigidbody != null)
            {
                Rigidbody.MoveRotation(Rigidbody.rotation + angle);
            }
        }
    }
}