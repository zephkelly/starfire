namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Interface for approach strategies that calculate steering toward waypoints.
    /// Different strategies produce different movement behaviors (smooth stop, fast brake, flyby, etc.)
    /// </summary>
    public interface IApproachStrategy
    {
        /// <summary>
        /// Calculate steering output for approaching a waypoint.
        /// </summary>
        /// <param name="context">Context containing position, velocity, waypoint data, etc.</param>
        /// <returns>Steering output with force to apply and distance to target.</returns>
        SteeringOutput Calculate(ApproachContext context);

        /// <summary>
        /// Check if arrival conditions are met for this strategy.
        /// Different strategies have different arrival criteria (e.g., flyby doesn't require stopping).
        /// </summary>
        /// <param name="context">The approach context.</param>
        /// <param name="output">The steering output from Calculate().</param>
        /// <returns>True if the agent has arrived according to this strategy's criteria.</returns>
        bool IsArrived(ApproachContext context, SteeringOutput output);
    }
}
