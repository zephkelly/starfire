using System;

namespace Starfire.Entity.Modules.Transponder
{
    [Flags]
    public enum CommChannel
    {
        None = 0,
        General = 1 << 0,
        Hail = 1 << 1,
        Private = 1 << 2,
        Emergency = 1 << 3,
        Military = 1 << 4,
        Trade = 1 << 5,
        Navigation = 1 << 6,

        AllPublic = General | Hail | Navigation,
        AllStandard = General | Hail | Private | Emergency | Navigation,
        All = ~0
    }
}
