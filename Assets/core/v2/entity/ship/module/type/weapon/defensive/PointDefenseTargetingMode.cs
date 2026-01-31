namespace StarfireV2
{
    /// <summary>
    /// Determines what a point defense module prioritizes targeting.
    /// </summary>
    public enum PointDefenseTargetingMode
    {
        /// <summary>
        /// 3-tier priority: guided threats > collision-course projectiles > hostile entities.
        /// </summary>
        Auto,

        /// <summary>
        /// Only engage threats (guided missiles, torpedoes, mines, and collision-course projectiles).
        /// Never targets hostile ships or stations.
        /// </summary>
        Defensive,

        /// <summary>
        /// Only engage hostile entities (ships, stations). Ignores all incoming threats.
        /// </summary>
        Offensive
    }
}
