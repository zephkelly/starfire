using UnityEngine;

namespace StarfireV2
{
    public interface IEntityController
    {
        public IEntity Entity { get; }
        public EntityControllerDriverStack DriverStack { get; }

        public Rigidbody2D Rigid2D { get; }
        public Transform Transform { get; }
    }
}