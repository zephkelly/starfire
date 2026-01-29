namespace StarfireV2
{
    /// <summary>
    /// Categorizes the type of entity detected by sensors.
    /// </summary>
    public enum V2DetectedEntityType
    {
        /// <summary>Entity type could not be determined.</summary>
        Unknown = 0,

        /// <summary>Standard ship entity.</summary>
        Ship = 1,

        /// <summary>Space station or installation.</summary>
        Station = 2,

        /// <summary>Asteroid or debris.</summary>
        Asteroid = 3,

        // Threat types (value >= 10 indicates a threat)
        /// <summary>Standard projectile (bullets, lasers).</summary>
        Projectile = 10,

        /// <summary>Guided missile.</summary>
        Missile = 11,

        /// <summary>Heavy torpedo.</summary>
        Torpedo = 12,

        /// <summary>Mine or proximity explosive.</summary>
        Mine = 13
    }
}
