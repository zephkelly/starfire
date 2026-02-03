using System;
using System.Collections.Generic;
using Starfire.Core.V2.World;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Types of simplified behaviors that can be predicted analytically
    /// without per-frame simulation.
    /// </summary>
    public enum SimulatedBehaviorType
    {
        /// <summary>Pure inertia + drag (asteroids, debris, unpowered ships).</summary>
        Ballistic,

        /// <summary>Heading toward a destination point at max speed.</summary>
        GoToPosition,

        /// <summary>Following a waypoint sequence, stopping at each.</summary>
        FollowPath,

        /// <summary>Orbiting a point at fixed radius.</summary>
        Orbit,

        /// <summary>Cycling between patrol waypoints indefinitely.</summary>
        Patrol,

        /// <summary>Moving away from a threat position.</summary>
        Flee,

        /// <summary>Stationary, no movement.</summary>
        Idle
    }

    /// <summary>
    /// Captures the simplified behavior state of a ship when it enters background simulation.
    /// This allows deterministic prediction of position without running the full AI system.
    /// </summary>
    [Serializable]
    public class BehaviorSnapshot
    {
        /// <summary>The type of behavior to simulate.</summary>
        public SimulatedBehaviorType Type = SimulatedBehaviorType.Ballistic;

        /// <summary>Target position for GoToPosition, Flee behaviors.</summary>
        public Vector2D TargetPosition;

        /// <summary>Waypoints for FollowPath, Patrol behaviors.</summary>
        public List<Vector2D> Waypoints;

        /// <summary>Current waypoint index for path/patrol behaviors.</summary>
        public int CurrentWaypointIndex;

        /// <summary>Whether patrol loops back to start.</summary>
        public bool Loop = true;

        /// <summary>Center point for Orbit behavior.</summary>
        public Vector2D OrbitCenter;

        /// <summary>Orbit radius for Orbit behavior.</summary>
        public float OrbitRadius;

        /// <summary>Orbit speed in radians per second (positive = counter-clockwise).</summary>
        public float OrbitSpeed;

        // Ship capabilities (captured from ShipConfiguration)

        /// <summary>Maximum speed the ship can travel.</summary>
        public float MaxSpeed;

        /// <summary>Acceleration in units per second squared.</summary>
        public float Acceleration;

        /// <summary>Turn rate in degrees per second.</summary>
        public float TurnRate;

        /// <summary>
        /// Create a ballistic (coasting) behavior snapshot.
        /// </summary>
        public static BehaviorSnapshot Ballistic() => new() { Type = SimulatedBehaviorType.Ballistic };

        /// <summary>
        /// Create a GoToPosition behavior snapshot.
        /// </summary>
        public static BehaviorSnapshot GoTo(Vector2D target, float maxSpeed)
            => new()
            {
                Type = SimulatedBehaviorType.GoToPosition,
                TargetPosition = target,
                MaxSpeed = maxSpeed
            };

        /// <summary>
        /// Create a Patrol behavior snapshot.
        /// </summary>
        public static BehaviorSnapshot PatrolRoute(List<Vector2D> waypoints, int currentIndex, float maxSpeed, bool loop = true)
            => new()
            {
                Type = SimulatedBehaviorType.Patrol,
                Waypoints = waypoints ?? new List<Vector2D>(),
                CurrentWaypointIndex = currentIndex,
                MaxSpeed = maxSpeed,
                Loop = loop
            };

        /// <summary>
        /// Create an Orbit behavior snapshot.
        /// </summary>
        public static BehaviorSnapshot OrbitPoint(Vector2D center, float radius, float speed)
            => new()
            {
                Type = SimulatedBehaviorType.Orbit,
                OrbitCenter = center,
                OrbitRadius = radius,
                OrbitSpeed = speed
            };

        /// <summary>
        /// Create a Flee behavior snapshot.
        /// </summary>
        public static BehaviorSnapshot FleeFrom(Vector2D threatPosition, float maxSpeed)
            => new()
            {
                Type = SimulatedBehaviorType.Flee,
                TargetPosition = threatPosition,
                MaxSpeed = maxSpeed
            };

        /// <summary>
        /// Create an Idle behavior snapshot.
        /// </summary>
        public static BehaviorSnapshot Idle() => new() { Type = SimulatedBehaviorType.Idle };
    }
}
