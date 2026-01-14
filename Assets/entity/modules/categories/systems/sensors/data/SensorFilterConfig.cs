using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Entity.Modules.Sensor
{
    [Serializable]
    public class SensorFilterConfig
    {
        public enum FilterMode
        {
            None,
            Include,
            Exclude
        }

        [Header("Faction Filtering")]
        public FilterMode factionFilterMode = FilterMode.None;
        public List<FactionData> factionList = new();

        [Header("Ship Class Filtering")]
        public FilterMode classFilterMode = FilterMode.None;
        public List<ShipClassDefinition> classList = new();

        [Header("Relation Filtering")]
        public bool excludeAllied = false;
        public bool excludeFriendly = false;
        public bool excludeNeutral = false;
        public bool excludeHostile = false;

        public bool PassesFilter(FactionData entityFaction, ShipClassDefinition entityClass, FactionData ownFaction)
        {
            if (!PassesFactionFilter(entityFaction))
                return false;

            if (!PassesClassFilter(entityClass))
                return false;

            if (!PassesRelationFilter(entityFaction, ownFaction))
                return false;

            return true;
        }

        private bool PassesFactionFilter(FactionData entityFaction)
        {
            if (factionFilterMode == FilterMode.None || factionList.Count == 0)
                return true;

            bool isInList = factionList.Contains(entityFaction);

            return factionFilterMode switch
            {
                FilterMode.Include => isInList,
                FilterMode.Exclude => !isInList,
                _ => true
            };
        }

        private bool PassesClassFilter(ShipClassDefinition entityClass)
        {
            if (classFilterMode == FilterMode.None || classList.Count == 0)
                return true;

            bool isInList = classList.Contains(entityClass);

            return classFilterMode switch
            {
                FilterMode.Include => isInList,
                FilterMode.Exclude => !isInList,
                _ => true
            };
        }

        private bool PassesRelationFilter(FactionData entityFaction, FactionData ownFaction)
        {
            if (ownFaction == null || entityFaction == null)
                return true;

            var relation = ownFaction.GetRelationTo(entityFaction);

            return relation switch
            {
                FactionRelationType.Allied when excludeAllied => false,
                FactionRelationType.Friendly when excludeFriendly => false,
                FactionRelationType.Neutral when excludeNeutral => false,
                FactionRelationType.Hostile when excludeHostile => false,
                FactionRelationType.Unfriendly when excludeHostile => false,
                _ => true
            };
        }
    }
}
