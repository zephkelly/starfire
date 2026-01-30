using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for SetCruiseSpeedAction.
    /// Sets cruise speed/acceleration limits on the blackboard for steering actions to use.
    /// </summary>
    [Serializable]
    public class SetCruiseSpeedParameters : IBTNodeParameters
    {
        /// <summary>
        /// Cruise speed to set. -1 means use ship's max speed.
        /// </summary>
        public float speed = -1f;

        /// <summary>
        /// Cruise acceleration to set. -1 means use ship's max acceleration.
        /// </summary>
        public float acceleration = -1f;

        /// <summary>
        /// Blackboard key to store the cruise speed.
        /// </summary>
        public string speedKey = "cruise_speed";

        /// <summary>
        /// Blackboard key to store the cruise acceleration.
        /// </summary>
        public string accelKey = "cruise_acceleration";
    }
}
