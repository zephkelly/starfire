using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Base class for goal configuration assets.
    /// Goal parameters write configuration to the BT blackboard before execution,
    /// enabling the same behavior tree to be reused with different configurations.
    /// </summary>
    public abstract class GoalParameters : ScriptableObject
    {
        /// <summary>
        /// Writes this goal's configuration values to the blackboard.
        /// Called by the AI system before behavior tree execution begins.
        /// </summary>
        /// <param name="context">The BT context containing the blackboard.</param>
        public abstract void WriteToBlackboard(BTContext context);

        /// <summary>
        /// Clears this goal's configuration values from the blackboard.
        /// Called when the goal is completed or abandoned.
        /// </summary>
        /// <param name="context">The BT context containing the blackboard.</param>
        public virtual void ClearFromBlackboard(BTContext context)
        {
            // Override in derived classes to clean up specific keys
        }
    }
}
