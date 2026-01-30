using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for ScanForContactsAction.
    /// Queries sensor module for detected entities at a minimum detection level.
    /// </summary>
    [Serializable]
    public class ScanForContactsParameters : IBTNodeParameters
    {
        /// <summary>
        /// Minimum detection level to consider.
        /// </summary>
        public V2DetectionLevel minDetectionLevel = V2DetectionLevel.Presence;

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int maxResults = 10;

        /// <summary>
        /// Blackboard key to write the results list.
        /// </summary>
        public string outputKey = "sensor_contacts";

        /// <summary>
        /// Blackboard key for identified entities list to exclude.
        /// </summary>
        public string excludeIdentifiedKey = "identified_entities";
    }
}
