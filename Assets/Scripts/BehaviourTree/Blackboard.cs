using System.Collections.Generic;

namespace Starfire
{
    public class Blackboard
    {
        private Blackboard fleetBlackboard;
        private Dictionary<string, object> blackboard = new Dictionary<string, object>();

        public Blackboard FleetBlackboard => fleetBlackboard;

        public void SetFleetBlackboard(Blackboard _fleetBlackboard)
        {
            fleetBlackboard = _fleetBlackboard;
        }

        public void SetValue(string key, object value)
        {
            if (blackboard.ContainsKey(key))
            {
                blackboard[key] = value;
            }
            else
            {
                blackboard.Add(key, value);
            }
        }

        public object GetValue(string key)
        {
            if (blackboard.ContainsKey(key))
            {
                return blackboard[key];
            }
            else
            {
                return null;
            }
        }

        public void RemoveValue(string key)
        {
            if (blackboard.ContainsKey(key))
            {
                blackboard.Remove(key);
            }
        }
    }
}