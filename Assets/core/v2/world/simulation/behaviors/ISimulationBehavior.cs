using Starfire.Core.V2.World;

namespace Starfire.Core.V2.World.Simulation.Behaviors
{
    /// <summary>
    /// Runtime interface for simulation behaviors.
    /// Implementations handle per-frame updates (Tier 1) and analytical prediction (Tier 2).
    /// Created from SimulationBehaviorConfig via CreateBehavior().
    /// </summary>
    public interface ISimulationBehavior
    {
        /// <summary>
        /// The type of behavior this instance represents.
        /// </summary>
        SimulatedBehaviorType Type { get; }

        /// <summary>
        /// The configuration this behavior was created from.
        /// </summary>
        SimulationBehaviorConfig Config { get; }

        /// <summary>
        /// Update an entity's state for one simulation frame (Tier 1).
        /// Called every frame for entities in active simulation.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="deltaTime">Time since last update in seconds.</param>
        /// <param name="context">Simulation context for querying world state.</param>
        void UpdateEntity(SimulatedEntity entity, float deltaTime, ISimulationContext context);

        /// <summary>
        /// Predict an entity's position at a future time (Tier 2).
        /// Called when querying an entity's predicted position without per-frame simulation.
        /// </summary>
        /// <param name="entity">The entity to predict.</param>
        /// <param name="snapshot">The behavior snapshot captured when entering Tier 2.</param>
        /// <param name="elapsedTime">Time elapsed since the snapshot was taken.</param>
        /// <returns>Predicted absolute position.</returns>
        Vector2D PredictPosition(SimulatedEntity entity, BehaviorSnapshot snapshot, double elapsedTime);

        /// <summary>
        /// Predict an entity's velocity at a future time (Tier 2).
        /// </summary>
        /// <param name="entity">The entity to predict.</param>
        /// <param name="snapshot">The behavior snapshot captured when entering Tier 2.</param>
        /// <param name="elapsedTime">Time elapsed since the snapshot was taken.</param>
        /// <returns>Predicted velocity.</returns>
        Vector2D PredictVelocity(SimulatedEntity entity, BehaviorSnapshot snapshot, double elapsedTime);

        /// <summary>
        /// Predict an entity's rotation at a future time (Tier 2).
        /// </summary>
        /// <param name="entity">The entity to predict.</param>
        /// <param name="snapshot">The behavior snapshot captured when entering Tier 2.</param>
        /// <param name="elapsedTime">Time elapsed since the snapshot was taken.</param>
        /// <returns>Predicted rotation in degrees.</returns>
        float PredictRotation(SimulatedEntity entity, BehaviorSnapshot snapshot, double elapsedTime);

        /// <summary>
        /// Capture the current behavior state as a snapshot for Tier 2 prediction.
        /// Called when an entity is demoted from Tier 1 to Tier 2.
        /// </summary>
        /// <param name="entity">The entity to capture state from.</param>
        /// <param name="context">Simulation context for additional state.</param>
        /// <returns>A snapshot of the current behavior state.</returns>
        BehaviorSnapshot CaptureSnapshot(SimulatedEntity entity, ISimulationContext context);

        /// <summary>
        /// Restore behavior state from a snapshot when promoted back to Tier 1.
        /// Called when an entity returns from Tier 2 to Tier 1.
        /// </summary>
        /// <param name="entity">The entity to restore state to.</param>
        /// <param name="snapshot">The snapshot to restore from.</param>
        /// <param name="context">Simulation context.</param>
        void RestoreFromSnapshot(SimulatedEntity entity, BehaviorSnapshot snapshot, ISimulationContext context);

        /// <summary>
        /// Get the confidence level for predictions at a given elapsed time.
        /// Returns 1.0 for high confidence, approaching 0.0 for uncertain predictions.
        /// </summary>
        /// <param name="elapsedTime">Time elapsed since snapshot.</param>
        /// <returns>Confidence level between 0 and 1.</returns>
        float GetPredictionConfidence(double elapsedTime);
    }
}
