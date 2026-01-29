using System;
using System.Collections.Generic;
using Starfire.Entity;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for filtering sensor detection results.
    /// </summary>
    [Serializable]
    public class V2SensorFilterConfig
    {
        public enum FilterMode
        {
            /// <summary>No filtering applied.</summary>
            None,
            /// <summary>Only include items in the list.</summary>
            Include,
            /// <summary>Exclude items in the list.</summary>
            Exclude
        }

        [Header("Faction Filtering")]
        [Tooltip("How to filter by faction.")]
        [SerializeField] private FilterMode factionFilterMode = FilterMode.None;

        [Tooltip("List of factions to include or exclude based on filter mode.")]
        [SerializeField] private List<V2FactionData> factionList = new();

        [Header("Ship Class Filtering")]
        [Tooltip("How to filter by ship class.")]
        [SerializeField] private FilterMode classFilterMode = FilterMode.None;

        [Tooltip("List of ship classes to include or exclude based on filter mode.")]
        [SerializeField] private List<ShipClassDefinition> classList = new();

        [Header("Relation-Based Filtering")]
        [Tooltip("Exclude allied entities from results.")]
        [SerializeField] private bool excludeAllied = false;

        [Tooltip("Exclude friendly entities from results.")]
        [SerializeField] private bool excludeFriendly = false;

        [Tooltip("Exclude neutral entities from results.")]
        [SerializeField] private bool excludeNeutral = false;

        [Tooltip("Exclude hostile entities from results.")]
        [SerializeField] private bool excludeHostile = false;

        /// <summary>
        /// Checks if an entity passes all configured filters.
        /// </summary>
        /// <param name="entityFaction">The faction of the detected entity.</param>
        /// <param name="entityClass">The ship class of the detected entity.</param>
        /// <param name="ownFaction">The faction of the scanning entity.</param>
        /// <returns>True if the entity passes all filters, false if it should be excluded.</returns>
        public bool PassesFilter(V2FactionData entityFaction, ShipClassDefinition entityClass, V2FactionData ownFaction)
        {
            // Faction list filter
            if (factionFilterMode == FilterMode.Include)
            {
                if (entityFaction == null || !factionList.Contains(entityFaction))
                    return false;
            }
            else if (factionFilterMode == FilterMode.Exclude)
            {
                if (entityFaction != null && factionList.Contains(entityFaction))
                    return false;
            }

            // Class list filter
            if (classFilterMode == FilterMode.Include)
            {
                if (entityClass == null || !classList.Contains(entityClass))
                    return false;
            }
            else if (classFilterMode == FilterMode.Exclude)
            {
                if (entityClass != null && classList.Contains(entityClass))
                    return false;
            }

            // Relation-based filters
            if (ownFaction != null && entityFaction != null)
            {
                var relation = ownFaction.GetRelationTo(entityFaction);

                if (excludeAllied && relation == V2FactionRelationType.Allied)
                    return false;

                if (excludeFriendly && relation == V2FactionRelationType.Friendly)
                    return false;

                if (excludeNeutral && (relation == V2FactionRelationType.Neutral || relation == V2FactionRelationType.Unknown))
                    return false;

                if (excludeHostile && (relation == V2FactionRelationType.Hostile || relation == V2FactionRelationType.Unfriendly))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a default filter configuration that allows all entities.
        /// </summary>
        public static V2SensorFilterConfig CreateDefault()
        {
            return new V2SensorFilterConfig
            {
                factionFilterMode = FilterMode.None,
                classFilterMode = FilterMode.None,
                excludeAllied = false,
                excludeFriendly = false,
                excludeNeutral = false,
                excludeHostile = false
            };
        }

        /// <summary>
        /// Creates a filter that only detects hostile entities.
        /// </summary>
        public static V2SensorFilterConfig CreateHostilesOnly()
        {
            return new V2SensorFilterConfig
            {
                factionFilterMode = FilterMode.None,
                classFilterMode = FilterMode.None,
                excludeAllied = true,
                excludeFriendly = true,
                excludeNeutral = true,
                excludeHostile = false
            };
        }
    }
}
