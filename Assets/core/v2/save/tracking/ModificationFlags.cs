using System;

namespace Starfire.Core.V2.Save.Tracking
{
    [Flags]
    public enum ChunkModificationFlags : byte
    {
        None = 0,
        AsteroidsAdded = 1 << 0,
        AsteroidsRemoved = 1 << 1,
        AsteroidsModified = 1 << 2,
        NebulaModified = 1 << 3,
        CelestialBodyModified = 1 << 4,
    }

    [Flags]
    public enum EntityModificationFlags : ushort
    {
        None = 0,
        PositionChanged = 1 << 0,
        DamageTaken = 1 << 1,
        ModulesChanged = 1 << 2,
        FactionChanged = 1 << 3,
        DriverChanged = 1 << 4,
        Destroyed = 1 << 5,
        SpawnedByPlayer = 1 << 6,
    }
}
