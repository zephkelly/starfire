using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Represents an entity detected by sensors.
    /// Information availability is gated by detection level.
    /// </summary>
    public struct V2DetectedEntity
    {
        /// <summary>Transform of the detected entity.</summary>
        public Transform Transform;

        /// <summary>Rigidbody of the detected entity (for velocity calculations).</summary>
        public Rigidbody2D Rigidbody;

        /// <summary>The type of entity detected.</summary>
        public V2DetectedEntityType EntityType;

        /// <summary>Current detection level for this entity.</summary>
        public V2DetectionLevel Level;

        /// <summary>Distance from the sensor to this entity.</summary>
        public float Distance;

        /// <summary>Time when this entity was last updated.</summary>
        public float LastUpdateTime;

        /// <summary>Whether this entity has a transponder that is transmitting.</summary>
        public bool HasActiveTransponder;

        // Faction and class data - only accessible at Silhouette+ level
        private V2FactionData _faction;
        private string _shipClassName;

        // Full transponder data - only accessible at Full level
        private V2TransponderData? _fullData;

        /// <summary>Returns true if this detection is valid (transform exists).</summary>
        public bool IsValid => Transform != null;

        /// <summary>Returns true if this entity is a threat type (projectile, missile, etc.).</summary>
        public bool IsThreat => EntityType >= V2DetectedEntityType.Projectile;

        /// <summary>Current world position of the detected entity.</summary>
        public Vector2 Position => Transform != null ? (Vector2)Transform.position : Vector2.zero;

        /// <summary>Current velocity of the detected entity.</summary>
        public Vector2 Velocity => Rigidbody != null ? Rigidbody.linearVelocity : Vector2.zero;

        /// <summary>
        /// Faction of the detected entity. Returns null if detection level is below Silhouette.
        /// </summary>
        public V2FactionData Faction => Level >= V2DetectionLevel.Silhouette ? _faction : null;

        /// <summary>
        /// Ship class name of the detected entity. Returns "Unknown" if detection level is below Silhouette.
        /// </summary>
        public string ShipClassName => Level >= V2DetectionLevel.Silhouette ? _shipClassName : "Unknown";

        /// <summary>
        /// Full transponder data. Returns null if detection level is below Full.
        /// </summary>
        public V2TransponderData? FullData => Level >= V2DetectionLevel.Full ? _fullData : null;

        /// <summary>
        /// Sets the faction data (used internally by sensor during detection).
        /// </summary>
        internal void SetFactionData(V2FactionData faction, string className)
        {
            _faction = faction;
            _shipClassName = className;
        }

        /// <summary>
        /// Sets the full transponder data (used internally by sensor during detection).
        /// </summary>
        internal void SetFullData(V2TransponderData data)
        {
            _fullData = data;
        }

        /// <summary>
        /// Creates a detection result for a threat (missile, projectile, etc.).
        /// </summary>
        public static V2DetectedEntity CreateThreat(
            Transform transform,
            Rigidbody2D rigidbody,
            V2DetectedEntityType type,
            float distance)
        {
            return new V2DetectedEntity
            {
                Transform = transform,
                Rigidbody = rigidbody,
                EntityType = type,
                Level = V2DetectionLevel.Presence, // Threats default to Presence level
                Distance = distance,
                LastUpdateTime = Time.time,
                HasActiveTransponder = false
            };
        }
    }
}
