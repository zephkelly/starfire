using System.Collections.Generic;

namespace Starfire
{
    public class Blackboard
    {
        public FleetBlackboard FleetBlackboard { get; private set; }

        private Dictionary<Ship, int> immediateThreats = new Dictionary<Ship, int>();
        private Ship currentTarget = null;

        public IReadOnlyDictionary<Ship, int> ImmediateThreats => immediateThreats;
        public Ship CurrentTarget => currentTarget;

        public void SetFleetBlackboard(FleetBlackboard _fleetBlackboard)
        {
            FleetBlackboard = _fleetBlackboard;
        }

        public void AddImmediateThreat(Ship threat, int damage)
        {
            if (!immediateThreats.ContainsKey(threat))
            {
                immediateThreats.Add(threat, damage);
            }
            else
            {
                immediateThreats[threat] += damage;
            
            }
        }

        public void RemoveImmediateThreat(Ship threat)
        {
            if (immediateThreats.ContainsKey(threat))
            {
                immediateThreats.Remove(threat);
            }
        }

        public void ClearImmediateThreats()
        {
            immediateThreats.Clear();
        }

        public void SetCurrentTarget(Ship target)
        {
            if (target is null || target == currentTarget) return;
            currentTarget = target;
        }

        public void ClearCurrentTarget()
        {
            currentTarget = null;
        }
    }
}