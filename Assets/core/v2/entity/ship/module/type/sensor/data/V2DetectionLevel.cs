namespace StarfireV2
{
    /// <summary>
    /// Defines the level of detail available when detecting an entity.
    /// Higher levels provide more information about the detected target.
    /// </summary>
    public enum V2DetectionLevel
    {
        /// <summary>Entity is not detected.</summary>
        None = 0,

        /// <summary>
        /// Basic presence detected. Only position is known.
        /// This is the passive detection level - always available when in range.
        /// </summary>
        Presence = 1,

        /// <summary>
        /// Silhouette detected. Faction and ship class are visible.
        /// Requires transponder OR close range passive detection.
        /// </summary>
        Silhouette = 2,

        /// <summary>
        /// Full detection. Complete transponder data is available.
        /// Requires active transponder OR very close range.
        /// </summary>
        Full = 3
    }
}
