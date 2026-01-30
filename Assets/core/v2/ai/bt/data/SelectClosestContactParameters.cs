using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for SelectClosestContactAction.
    /// Selects the closest contact from a list and stores as investigation target.
    /// </summary>
    [Serializable]
    public class SelectClosestContactParameters : IBTNodeParameters
    {
        /// <summary>
        /// Blackboard key containing the contacts list.
        /// </summary>
        public string inputKey = "sensor_contacts";

        /// <summary>
        /// Blackboard key to write the selected target.
        /// </summary>
        public string targetKey = "investigation_target";
    }
}
