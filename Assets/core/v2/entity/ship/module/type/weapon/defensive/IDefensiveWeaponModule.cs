namespace StarfireV2
{
    /// <summary>
    /// Interface for defensive weapon modules that protect against incoming threats.
    /// Typically includes point defense systems that auto-target projectiles and missiles.
    /// </summary>
    public interface IDefensiveWeaponModule : IWeaponModule
    {
        /// <summary>
        /// Maximum range at which threats can be engaged.
        /// </summary>
        float EngagementRange { get; }

        /// <summary>
        /// Speed at which the weapon tracks targets in degrees per second.
        /// </summary>
        float TrackingSpeed { get; }

        /// <summary>
        /// Whether the weapon is currently engaged with a target.
        /// </summary>
        bool IsEngaged { get; }

        /// <summary>
        /// Enables or disables autonomous target acquisition and firing.
        /// </summary>
        /// <param name="enabled">True to enable auto-targeting, false for manual control.</param>
        void SetAutoTargetingEnabled(bool enabled);

        /// <summary>
        /// Gets the number of targets currently being tracked.
        /// </summary>
        /// <returns>The count of tracked targets.</returns>
        int GetTrackedTargetCount();
    }
}
