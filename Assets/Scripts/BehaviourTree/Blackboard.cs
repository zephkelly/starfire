using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public class Blackboard
    {
        public FleetBlackboard FleetBlackboard { get; private set; }

        private Dictionary<Ship, int> immediateThreats = new Dictionary<Ship, int>();
        private object currentTarget = null;
        private Vector2 currentHeading = Vector2.zero;
    

        public IReadOnlyDictionary<Ship, int> ImmediateThreats => immediateThreats;
        public object CurrentTarget => currentTarget;
        public Vector2 CurrentHeading => currentHeading;

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

        public void SetCurrentTarget<T>(T newTarget)
        {
            currentTarget = newTarget;
        }

        public void SetCurrentHeadingAndNormalise(Vector2 newHeading)
        {
            currentHeading = newHeading.normalized;
        }

        public void ClearCurrentTarget()
        {
            currentTarget = null;
        }

        public Vector2 GetCurrentTargetPosition()
        {
            switch (currentTarget)
            {
                case Ship _ship:
                    return _ship.Controller.ShipTransform.position;
                case Vector2 _vector2:
                    return _vector2;
                case Transform _transform:
                    return _transform.position;
                default:
                    Debug.LogWarning("MoveToTargetNode: Target is not a valid type.");
                    return Vector2.zero;
            }
        }
    }
}