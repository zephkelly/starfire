namespace StarfireV2
{
    /// <summary>
    /// Defines how a homing missile acquires its target.
    /// </summary>
    public enum V2MissileTargetingMode
    {
        /// <summary>
        /// Missile automatically finds the nearest valid enemy after launch.
        /// Uses Physics2D.OverlapCircleAll to scan for targets.
        /// </summary>
        AutoAcquire,

        /// <summary>
        /// Missile uses a pre-locked target passed from the weapon module.
        /// Falls back to AutoAcquire if no target is provided.
        /// </summary>
        PreLocked
    }
}
