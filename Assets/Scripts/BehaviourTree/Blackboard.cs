using System.Collections.Generic;

namespace Starfire
{
    public class Blackboard
    {
        private List<Ship> detectedThreats = new List<Ship>();
        public FleetBlackboard FleetBlackboard { get; private set; }

        public IReadOnlyList<Ship> DetectedThreats => detectedThreats.AsReadOnly();

        public void SetFleetBlackboard(FleetBlackboard _fleetBlackboard)
        {
            FleetBlackboard = _fleetBlackboard;
        }

        public void AddDetectedThreat(Ship threat)
        {
            if (!detectedThreats.Contains(threat))
            {
                detectedThreats.Add(threat);
            }
        }

        public void RemoveDetectedThreat(Ship threat)
        {
            if (detectedThreats.Contains(threat))
            {
                detectedThreats.Remove(threat);
            }
        }

        public void ClearDetectedThreats()
        {
            detectedThreats.Clear();
        }
    }
}