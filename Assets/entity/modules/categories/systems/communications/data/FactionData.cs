using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.Modules.Transponder
{
    [CreateAssetMenu(fileName = "NewFaction", menuName = "Starfire/Factions/Faction Data")]
    public class FactionData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string factionId;
        [SerializeField] private string factionName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite factionIcon;
        [SerializeField] private Color factionColor = Color.white;

        [Header("Relationships")]
        [SerializeField] private List<FactionRelationship> relationships = new();

        public string FactionId => factionId;
        public string FactionName => factionName;
        public string Description => description;
        public Sprite FactionIcon => factionIcon;
        public Color FactionColor => factionColor;
        public IReadOnlyList<FactionRelationship> Relationships => relationships;

        public FactionRelationType GetRelationTo(FactionData other)
        {
            if (other == null || other == this)
                return FactionRelationType.Allied;

            foreach (var relationship in relationships)
            {
                if (relationship.targetFaction == other)
                    return relationship.relationType;
            }

            return FactionRelationType.Unknown;
        }

        public bool IsHostileTo(FactionData other)
        {
            var relation = GetRelationTo(other);
            return relation == FactionRelationType.Hostile || relation == FactionRelationType.Unfriendly;
        }

        public bool IsAlliedWith(FactionData other)
        {
            var relation = GetRelationTo(other);
            return relation == FactionRelationType.Allied || relation == FactionRelationType.Friendly;
        }
    }
}
