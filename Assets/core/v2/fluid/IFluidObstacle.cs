using UnityEngine;

namespace StarfireV2.Fluid
{
    /// <summary>
    /// Interface for objects that can affect the fluid simulation.
    /// Implement this on any entity that should push/displace nebula gas.
    /// </summary>
    public interface IFluidObstacle
    {
        /// <summary>
        /// World position of the obstacle center.
        /// </summary>
        Vector2 Position { get; }

        /// <summary>
        /// World velocity of the obstacle.
        /// </summary>
        Vector2 Velocity { get; }

        /// <summary>
        /// Half-extents of the obstacle ellipse (x = width/2, y = height/2).
        /// </summary>
        Vector2 HalfExtents { get; }

        /// <summary>
        /// Rotation in radians.
        /// </summary>
        float Rotation { get; }

        /// <summary>
        /// Multiplier for how much this obstacle affects the fluid (0-1, typically 1).
        /// </summary>
        float VelocityScale { get; }

        /// <summary>
        /// Whether this obstacle is currently active and should affect the fluid.
        /// </summary>
        bool IsActive { get; }
    }
}
