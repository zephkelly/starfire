namespace StarfireV2
{
    /// <summary>
    /// Defines the relationship type between two factions.
    /// </summary>
    public enum V2FactionRelationType
    {
        /// <summary>Relationship is unknown or undefined.</summary>
        Unknown = 0,

        /// <summary>Factions are hostile and will attack on sight.</summary>
        Hostile = 1,

        /// <summary>Factions have tense relations but may not attack unprovoked.</summary>
        Unfriendly = 2,

        /// <summary>Factions have no particular relationship.</summary>
        Neutral = 3,

        /// <summary>Factions have positive relations and may cooperate.</summary>
        Friendly = 4,

        /// <summary>Factions are formally allied and will defend each other.</summary>
        Allied = 5
    }
}
