namespace Starfire.Entity.Modules
{
    /// <summary>
    /// Top-level module categorization for organizational and gameplay purposes.
    /// </summary>
    public enum ModuleCategory
    {
        /// <summary>Hull, Shield, Deflector - fundamental ship integrity</summary>
        Core,

        /// <summary>All movement-related modules (thrusters, impulse, FTL)</summary>
        Propulsion,

        /// <summary>Offensive and defensive weapon systems</summary>
        Weapons,

        /// <summary>Sensors, communications, automation</summary>
        Systems,

        /// <summary>Cargo, life support, auxiliary systems</summary>
        Utility
    }
}
