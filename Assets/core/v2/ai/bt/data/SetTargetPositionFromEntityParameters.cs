using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for SetTargetPositionFromEntityAction.
    /// Extracts position from a V2DetectedEntity and writes it as a Vector2.
    /// </summary>
    [Serializable]
    public class SetTargetPositionFromEntityParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the V2DetectedEntity to read position from.
        /// </summary>
        public string sourceKey = "monitored_target";

        /// <summary>
        /// Blackboard key to write the extracted position (Vector2).
        /// </summary>
        public string outputKey = "rotation_target";
    }
}
