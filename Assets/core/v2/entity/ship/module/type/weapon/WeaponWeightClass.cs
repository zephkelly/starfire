namespace StarfireV2
{
    /// <summary>
    /// Defines the weight class of weapons and hardpoints.
    /// Weapons can mount on equal or larger hardpoints (upward compatible).
    /// </summary>
    public enum WeaponWeightClass
    {
        /// <summary>
        /// Small weapons: point defense, light lasers.
        /// Can mount on Light, Medium, or Heavy hardpoints.
        /// </summary>
        Light = 0,

        /// <summary>
        /// Standard weapons: cannons, medium lasers.
        /// Can mount on Medium or Heavy hardpoints.
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Capital weapons: torpedo launchers, heavy cannons.
        /// Can only mount on Heavy hardpoints.
        /// </summary>
        Heavy = 2
    }
}
