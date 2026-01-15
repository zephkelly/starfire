using System;
using System.Collections.Generic;
using Starfire.Entity.Modules.Transponder;

namespace Starfire.Entity.Modules.Sensor
{
    public interface ISensorShipModule : IShipModule
    {
        float DetectionRange { get; }
        float TargetingAccuracy { get; }
        float PollingRate { get; }

        IReadOnlyList<DetectedEntity> DetectedEntities { get; }
        int DetectedCount { get; }

        IEnumerable<DetectedEntity> GetHostileEntities();
        IEnumerable<DetectedEntity> GetAlliedEntities();
        IEnumerable<DetectedEntity> GetEntitiesByFaction(FactionData faction);
        IEnumerable<DetectedEntity> GetEntitiesAtLevel(DetectionLevel minLevel);

        event Action<DetectedEntity> OnEntityDetected;
        event Action<DetectedEntity> OnEntityLost;

        void RefreshNow();
    }
}
