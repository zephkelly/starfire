using UnityEngine;

namespace StarfireV2
{
    public interface IEntityController
    {
        public IEntity Entity { get; }
        public IEntityControllerDriver Driver { get; }

        public Rigidbody2D Rigidbody2D { get; }
        public Transform Transform { get; }
    }
}