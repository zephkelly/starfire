using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Defines a one-directional relationship from one faction to another.
    /// </summary>
    [Serializable]
    public class V2FactionRelationship
    {
        [Tooltip("The faction this relationship is directed toward.")]
        public V2FactionData targetFaction;

        [Tooltip("The type of relationship with the target faction.")]
        public V2FactionRelationType relationType = V2FactionRelationType.Neutral;
    }
}
