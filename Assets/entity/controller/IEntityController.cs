using UnityEngine;

namespace Starfire.Entity
{
    public interface IEntityController
    {
        public void Move(Vector2 direction, float speed);
        public void Rotate(float angle);
        // public void Aim(Vector2 target);

        // public void Fire();

        // public void Warp(WarpInputData input);
        // public void Hyperdrive(WarpInputData input);
    }
}