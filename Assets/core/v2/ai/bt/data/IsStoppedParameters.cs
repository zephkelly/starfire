using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for IsStoppedCondition.
    /// Checks if entity velocity is below threshold.
    /// </summary>
    [Serializable]
    public class IsStoppedParameters : IBTNodeParameters
    {
        /// <summary>
        /// Velocity threshold below which entity is considered stopped.
        /// </summary>
        public float threshold = 0.5f;
    }
}
