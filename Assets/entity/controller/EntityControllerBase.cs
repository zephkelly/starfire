using UnityEngine;
using Starfire.Entity.Modules;

namespace Starfire.Entity
{
    public abstract class EntityControllerBase : MonoBehaviour, IEntityController
    {
        public Entity Entity { get; protected set; }
        public Rigidbody2D Rigidbody { get; protected set; }
        public ControllerDriverStack DriverStack { get; } = new();
        public abstract IEntitySystems Systems { get; }

        protected Camera MainCamera { get; private set; }

        protected virtual void Awake()
        {
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                Rigidbody = rb;
            }

            MainCamera = Camera.main;
        }

        protected virtual void Update()
        {
            var driver = DriverStack.GetActiveDriver();
            if (driver == null || Entity == null || Systems == null) return;

            ProcessMovement(driver);
            ProcessRotation(driver);

            Systems.UpdateAll(Time.deltaTime);
        }

        protected abstract void ProcessMovement(IControllerDriver driver);
        protected abstract void ProcessRotation(IControllerDriver driver);

        public virtual void Move(Vector2 direction, float speed)
        {
            if (Rigidbody != null)
            {
                Vector2 movement = direction.normalized * speed * Time.deltaTime;
                Rigidbody.AddForce(movement, ForceMode2D.Force);
            }
        }

        public virtual void ApplyRotation(float angle)
        {
            if (Rigidbody != null)
            {
                Rigidbody.rotation = angle;
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        protected Vector2 ScreenToWorldPosition(Vector2 screenPos)
        {
            if (MainCamera == null) return Vector2.zero;
            Vector3 worldPos = MainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            return new Vector2(worldPos.x, worldPos.y);
        }
    }
}
