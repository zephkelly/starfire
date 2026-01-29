using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// ScriptableObject defining a faction and its relationships to other factions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFaction", menuName = "StarfireV2/Factions/Faction Data")]
    public class V2FactionData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this faction.")]
        [SerializeField] private string factionId;

        [Tooltip("Display name of the faction.")]
        [SerializeField] private string factionName;

        [TextArea(2, 4)]
        [Tooltip("Description of the faction.")]
        [SerializeField] private string description;

        [Tooltip("Icon representing this faction.")]
        [SerializeField] private Sprite factionIcon;

        [Tooltip("Color associated with this faction for UI and identification.")]
        [SerializeField] private Color factionColor = Color.white;

        [Header("Relationships")]
        [Tooltip("List of relationships this faction has with other factions.")]
        [SerializeField] private List<V2FactionRelationship> relationships = new();

        // Public accessors
        public string FactionId => factionId;
        public string FactionName => factionName;
        public string Description => description;
        public Sprite FactionIcon => factionIcon;
        public Color FactionColor => factionColor;
        public IReadOnlyList<V2FactionRelationship> Relationships => relationships;

        /// <summary>
        /// Gets the relationship type this faction has toward another faction.
        /// </summary>
        public V2FactionRelationType GetRelationTo(V2FactionData other)
        {
            if (other == null || other == this)
                return V2FactionRelationType.Allied;

            foreach (var relationship in relationships)
            {
                if (relationship.targetFaction == other)
                    return relationship.relationType;
            }

            return V2FactionRelationType.Unknown;
        }

        /// <summary>
        /// Returns true if this faction is hostile toward the other faction.
        /// </summary>
        public bool IsHostileTo(V2FactionData other)
        {
            var relation = GetRelationTo(other);
            return relation == V2FactionRelationType.Hostile || relation == V2FactionRelationType.Unfriendly;
        }

        /// <summary>
        /// Returns true if this faction is allied or friendly with the other faction.
        /// </summary>
        public bool IsAlliedWith(V2FactionData other)
        {
            var relation = GetRelationTo(other);
            return relation == V2FactionRelationType.Allied || relation == V2FactionRelationType.Friendly;
        }

        /// <summary>
        /// Returns true if this faction is neutral toward the other faction.
        /// </summary>
        public bool IsNeutralTo(V2FactionData other)
        {
            var relation = GetRelationTo(other);
            return relation == V2FactionRelationType.Neutral || relation == V2FactionRelationType.Unknown;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(factionId))
            {
                factionId = name;
            }

            if (string.IsNullOrEmpty(factionName))
            {
                factionName = name;
            }
        }
    }
}
