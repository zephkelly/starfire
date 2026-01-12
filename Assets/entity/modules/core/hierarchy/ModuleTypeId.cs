namespace Starfire.Entity.Modules
{
    /// <summary>
    /// Specific module type identifier for the new hierarchy system.
    /// Each value maps to a specific module interface.
    /// Values are grouped by ranges for organizational clarity.
    /// </summary>
    public enum ModuleTypeId
    {
        // === Core > Structure (0-9) ===
        Hull = 0,

        // === Core > Defense (10-19) ===
        Shield = 10,
        Deflector = 11,

        // === Propulsion > Maneuvering (100-109) ===
        ManeuveringThruster = 100,

        // === Propulsion > Impulse (110-119) ===
        ImpulseEngine = 110,

        // === Propulsion > FTL (120-129) ===
        WarpDrive = 120,
        Hyperdrive = 121,

        // === Weapons > Offensive (200-209) ===
        Laser = 200,
        PlasmaCannon = 201,
        MissileLauncher = 202,

        // === Weapons > Defensive (210-219) ===
        PointDefenseTurret = 210,

        // === Systems > Sensors (300-309) ===
        SensorArray = 300,

        // === Systems > Communications (310-319) ===
        Transponder = 310,

        // === Systems > Automation (320-329) ===
        AICore = 320,

        // === Utility > Storage (400-409) ===
        CargoBay = 400,

        // === Utility > Support (410-419) ===
        LifeSupport = 410
    }
}
