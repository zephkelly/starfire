namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Interface for steering behaviors that calculate movement forces.
    /// </summary>
    public interface ISteeringBehavior
    {
        /// <summary>
        /// Calculate steering output based on the current context.
        /// </summary>
        SteeringOutput Calculate(SteeringContext context);
    }
}
