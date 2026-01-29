using System;
using System.Collections.Generic;

namespace StarfireV2
{
    /// <summary>
    /// Interface for sensor modules that detect entities and threats.
    /// Sensors provide dual detection: passive (always active) and transponder-enhanced.
    /// </summary>
    public interface ISensorModule : IShipModule
    {
        /// <summary>Maximum range for presence detection.</summary>
        float DetectionRange { get; }

        /// <summary>Range at which Silhouette level detection is available with transponder.</summary>
        float SilhouetteRange { get; }

        /// <summary>Range at which Full level detection is available with transponder.</summary>
        float FullRange { get; }

        /// <summary>Targeting accuracy multiplier for weapon systems.</summary>
        float TargetingAccuracy { get; }

        /// <summary>How often the sensor updates its detection list (in seconds).</summary>
        float PollingRate { get; }

        /// <summary>List of all currently detected entities.</summary>
        IReadOnlyList<V2DetectedEntity> DetectedEntities { get; }

        /// <summary>Number of currently detected entities.</summary>
        int DetectedCount { get; }

        // Query methods - ships/entities
        /// <summary>Gets all detected entities that are hostile to the sensor's owner.</summary>
        IEnumerable<V2DetectedEntity> GetHostileEntities();

        /// <summary>Gets all detected entities that are allied with the sensor's owner.</summary>
        IEnumerable<V2DetectedEntity> GetAlliedEntities();

        /// <summary>Gets all detected entities belonging to a specific faction.</summary>
        IEnumerable<V2DetectedEntity> GetEntitiesByFaction(V2FactionData faction);

        /// <summary>Gets all detected entities at or above a minimum detection level.</summary>
        IEnumerable<V2DetectedEntity> GetEntitiesAtLevel(V2DetectionLevel minLevel);

        // Query methods - threats (missiles, projectiles)
        /// <summary>Gets all detected threats (missiles, projectiles, etc.).</summary>
        IEnumerable<V2DetectedEntity> GetDetectedThreats();

        /// <summary>Gets all detected threats within a specific range.</summary>
        IEnumerable<V2DetectedEntity> GetThreatsInRange(float range);

        // Events
        /// <summary>Fired when a new entity is detected.</summary>
        event Action<V2DetectedEntity> OnEntityDetected;

        /// <summary>Fired when an entity is no longer detected.</summary>
        event Action<V2DetectedEntity> OnEntityLost;

        /// <summary>Fired when a new threat is detected (for point defense integration).</summary>
        event Action<V2DetectedEntity> OnThreatDetected;

        /// <summary>Forces an immediate sensor scan instead of waiting for the poll interval.</summary>
        void RefreshNow();
    }
}
