using UnityEngine;

namespace Starfire.Core.V2.World.Simulation.Behaviors
{
    /// <summary>
    /// Base ScriptableObject for defining simulation behavior configurations.
    /// Derive from this class to create specific behavior types (Ballistic, Patrol, Homing, etc.).
    /// Each config can create runtime behavior instances that implement ISimulationBehavior.
    /// </summary>
    public abstract class SimulationBehaviorConfig : ScriptableObject
    {
        [Header("Behavior Identity")]
        [Tooltip("Human-readable name for this behavior.")]
        public string BehaviorName;

        [Tooltip("The type of behavior this config represents.")]
        public SimulatedBehaviorType BehaviorType;

        [TextArea(2, 4)]
        [Tooltip("Description of what this behavior does.")]
        public string Description;

        [Header("Tier 2 Prediction")]
        [Tooltip("Whether this behavior supports analytical prediction in Tier 2.")]
        public bool SupportsTier2Prediction = true;

        [Tooltip("How confident predictions are over time (0-1). Lower values mean faster degradation.")]
        [Range(0f, 1f)]
        public float PredictionConfidence = 0.9f;

        [Tooltip("Maximum time in seconds to predict ahead. Beyond this, use Tier 1 or mark uncertain.")]
        [Min(0f)]
        public float MaxPredictionTime = 60f;

        /// <summary>
        /// Create a runtime behavior instance from this configuration.
        /// The runtime instance implements ISimulationBehavior and performs actual simulation.
        /// </summary>
        /// <returns>A new runtime behavior instance.</returns>
        public abstract ISimulationBehavior CreateBehavior();

        /// <summary>
        /// Validate the configuration. Override in derived classes for custom validation.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(BehaviorName))
            {
                BehaviorName = BehaviorType.ToString();
            }
        }
    }
}
