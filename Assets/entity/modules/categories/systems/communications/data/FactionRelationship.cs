using System;

namespace Starfire.Entity.Modules.Transponder
{
    public enum FactionRelationType
    {
        Unknown = 0,
        Hostile = 1,
        Unfriendly = 2,
        Neutral = 3,
        Friendly = 4,
        Allied = 5
    }

    [Serializable]
    public class FactionRelationship
    {
        public FactionData targetFaction;
        public FactionRelationType relationType = FactionRelationType.Neutral;
    }
}
