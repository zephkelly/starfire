namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Types of approach strategies for waypoint navigation.
    /// </summary>
    public enum ApproachStrategyType
    {
        /// <summary>
        /// Proportional slowing as distance decreases, full stop at waypoint.
        /// Best for: Patrol, docking, precise positioning.
        /// </summary>
        Controlled,

        /// <summary>
        /// Full throttle until calculated braking point, then hard brake.
        /// Best for: Urgent travel, time-critical objectives.
        /// </summary>
        FastBrake,

        /// <summary>
        /// Maintain speed through waypoint, angle trajectory toward next waypoint.
        /// Best for: Racing, smooth multi-waypoint paths.
        /// </summary>
        Flyby
    }
}
