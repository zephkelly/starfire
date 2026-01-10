using UnityEngine;

namespace Starfire.Entity
{
    public interface IEntityController
    {
        Entity Entity { get; }
        Rigidbody2D Rigidbody { get; }
        ControllerDriverStack DriverStack { get; }
        IEntitySystems Systems { get; }

        void Move(Vector2 direction, float speed);
        void ApplyRotation(float angle);
    }
}