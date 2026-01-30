using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for HasInvestigationTargetCondition.
    /// Checks if an investigation target exists in blackboard.
    /// </summary>
    [Serializable]
    public class HasInvestigationTargetParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key to check.
        /// </summary>
        public string targetKey = "investigation_target";

        /// <summary>
        /// When true, inverts the condition (returns Success when target does NOT exist).
        /// </summary>
        public bool invert = false;
    }
}
