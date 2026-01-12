namespace Starfire.Entity.Modules
{
    /// <summary>
    /// Second-level grouping within categories.
    /// Used for organizing modules and applying category-specific limits.
    /// </summary>
    public enum ModuleSubCategory
    {
        // === Core Category ===

        /// <summary>Hull modules - structural integrity</summary>
        Structure,

        /// <summary>Shield and Deflector modules - damage mitigation</summary>
        Defense,

        // === Propulsion Category ===

        /// <summary>Rotation/thruster modules - fine maneuvering control</summary>
        Maneuvering,

        /// <summary>Standard propulsion - normal space travel</summary>
        Impulse,

        /// <summary>Faster-than-light drives (Warp, Hyperdrive)</summary>
        FTL,

        // === Weapons Category ===

        /// <summary>Primary attack weapons (lasers, missiles, plasma)</summary>
        Offensive,

        /// <summary>Defensive weapons (point defense turrets)</summary>
        Defensive,

        // === Systems Category ===

        /// <summary>Detection and targeting systems</summary>
        Sensors,

        /// <summary>Transponder and communication modules</summary>
        Communications,

        /// <summary>AI core and automation modules</summary>
        Automation,

        // === Utility Category ===

        /// <summary>Cargo bay modules</summary>
        Storage,

        /// <summary>Life support and crew systems</summary>
        Support
    }
}
