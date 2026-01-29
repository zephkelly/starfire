using System;

namespace StarfireV2
{
    /// <summary>
    /// Communication channels that transponders can broadcast on and monitor.
    /// </summary>
    [Flags]
    public enum V2CommChannel
    {
        None = 0,

        /// <summary>General broadcast channel for all ships.</summary>
        General = 1 << 0,

        /// <summary>Hailing channel for ship-to-ship contact.</summary>
        Hail = 1 << 1,

        /// <summary>Private encrypted channel.</summary>
        Private = 1 << 2,

        /// <summary>Emergency distress channel.</summary>
        Emergency = 1 << 3,

        /// <summary>Military encrypted channel.</summary>
        Military = 1 << 4,

        /// <summary>Trade and commerce channel.</summary>
        Trade = 1 << 5,

        /// <summary>Navigation and waypoint channel.</summary>
        Navigation = 1 << 6,

        /// <summary>All public channels (General, Hail, Navigation).</summary>
        AllPublic = General | Hail | Navigation,

        /// <summary>All standard channels (excludes Military).</summary>
        AllStandard = General | Hail | Private | Emergency | Navigation | Trade,

        /// <summary>All channels.</summary>
        All = ~0
    }
}
