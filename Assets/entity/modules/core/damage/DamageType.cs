namespace Starfire.Entity.Modules.Damage
{
    /// <summary>
    /// Base damage types that determine resistance calculations.
    /// </summary>
    public enum DamageType
    {
        /// <summary>Standard physical/kinetic damage from projectiles.</summary>
        Kinetic,

        /// <summary>Energy-based damage (lasers, phasers).</summary>
        Energy,

        /// <summary>Explosive damage (missiles, torpedoes).</summary>
        Explosive,

        /// <summary>Electromagnetic damage (ion weapons, EMPs).</summary>
        Electromagnetic,

        /// <summary>Plasma-based damage.</summary>
        Plasma,

        /// <summary>Direct damage that bypasses all resistances.</summary>
        True
    }
}
