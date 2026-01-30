using System;

namespace StarfireV2
{
    /// <summary>
    /// Parameters for BTRepeater decorator node.
    /// </summary>
    [Serializable]
    public class RepeaterParameters : IBTNodeParameters
    {
        /// <summary>
        /// Number of times to repeat. -1 means repeat forever.
        /// </summary>
        public int repeatCount = -1;
    }
}
