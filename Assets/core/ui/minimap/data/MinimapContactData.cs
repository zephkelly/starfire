using UnityEngine;
using Starfire.Entity;
using Starfire.Entity.Modules.Sensor;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Core.UI.Minimap
{
    /// <summary>
    /// Processed contact data ready for minimap display.
    /// </summary>
    public readonly struct MinimapContactData
    {
        /// <summary>
        /// Instance ID for identifying this entity in pooling.
        /// </summary>
        public readonly int EntityId;

        /// <summary>
        /// Absolute world position of the contact.
        /// </summary>
        public readonly Vector2 WorldPosition;

        /// <summary>
        /// Position relative to the sensor source (player).
        /// </summary>
        public readonly Vector2 RelativePosition;

        /// <summary>
        /// Distance from the sensor source.
        /// </summary>
        public readonly float Distance;

        /// <summary>
        /// Distance normalized to sensor range (0-1, can exceed 1 if beyond range).
        /// </summary>
        public readonly float NormalizedDistance;

        /// <summary>
        /// Sensor detection level for this contact.
        /// </summary>
        public readonly DetectionLevel Level;

        /// <summary>
        /// Faction data (null if below Silhouette detection level).
        /// </summary>
        public readonly FactionData Faction;

        /// <summary>
        /// Relationship between player and this contact.
        /// </summary>
        public readonly FactionRelationType Relationship;

        /// <summary>
        /// Ship class definition (null if below Silhouette detection level).
        /// </summary>
        public readonly ShipClassDefinition ShipClass;

        /// <summary>
        /// Whether this contact data is valid (entity still exists).
        /// </summary>
        public readonly bool IsValid;

        /// <summary>
        /// Reference to the entity controller (for extended info access).
        /// </summary>
        public readonly EntityControllerBase Controller;

        public MinimapContactData(
            DetectedEntity detected,
            Vector2 sourcePosition,
            float maxRange,
            FactionData playerFaction)
        {
            Controller = detected.Controller;
            IsValid = detected.IsValid;
            EntityId = detected.Controller != null ? detected.Controller.GetInstanceID() : 0;
            Level = detected.Level;
            Distance = detected.Distance;

            if (detected.IsValid)
            {
                WorldPosition = detected.Position;
                RelativePosition = WorldPosition - sourcePosition;
                NormalizedDistance = maxRange > 0 ? Distance / maxRange : 0f;
            }
            else
            {
                WorldPosition = Vector2.zero;
                RelativePosition = Vector2.zero;
                NormalizedDistance = 0f;
            }

            // Only available at Silhouette+ level
            Faction = detected.Faction;
            ShipClass = detected.ShipClass;

            // Determine relationship
            if (playerFaction != null && Faction != null)
            {
                Relationship = playerFaction.GetRelationTo(Faction);
            }
            else
            {
                Relationship = FactionRelationType.Unknown;
            }
        }
    }
}
