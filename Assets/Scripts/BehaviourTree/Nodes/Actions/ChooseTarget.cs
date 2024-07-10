using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public class ChooseTarget : Node
    {
        private Ship ship;

        private float nearestThreatThreshold = 100;
        private float biggestThreatThreshold = 300;

        public ChooseTarget(Ship ship)
        {
            this.ship = ship;
        }

        protected override NodeState OnEvaluate()
        {
            Ship nearestThreat = FindNearestThreat();

            if (nearestThreat is null)
            {
                return NodeState.Failure;
            }

            Ship biggestThreat = FindBiggestThreat();

            if (biggestThreat is null)
            {
                return NodeState.Failure;
            }

            Ship newCurrentTarget = DetermineTarget(nearestThreat, biggestThreat);
            ship.AICore.SetTarget(newCurrentTarget);

            ship.AICore.Blackboard.SetCurrentTarget(newCurrentTarget);
            return NodeState.Success;
        }

        private Ship DetermineTarget(Ship nearestThreat, Ship biggestThreat)
        {
            if (biggestThreat == nearestThreat)
            {
                return biggestThreat;
            }

            float nearestThreatDistance = Vector2.Distance(nearestThreat.Controller.ShipTransform.position, ship.Controller.ShipTransform.position);
            float biggestThreatDistance = Vector2.Distance(biggestThreat.Controller.ShipTransform.position, ship.Controller.ShipTransform.position);

            if (nearestThreatDistance < nearestThreatThreshold && biggestThreatDistance > biggestThreatThreshold)
            {
                return nearestThreat;
            }
            else if (nearestThreatDistance < nearestThreatThreshold && nearestThreatDistance < biggestThreatThreshold)
            {
                return biggestThreat;
            }
            else if (nearestThreatDistance > nearestThreatThreshold && biggestThreatDistance < biggestThreatThreshold)
            {
                return biggestThreat;
            }
            else if (nearestThreatDistance > nearestThreatThreshold && biggestThreatDistance > biggestThreatThreshold)
            {
                return nearestThreat;
            }

    
            return biggestThreat;
        }

        private Ship FindNearestThreat()
        {
            Ship closestShip = null;

            if (ship.AICore.Blackboard.ImmediateThreats.Count == 0)
            {
                return null;
            }

            foreach (var threat in ship.AICore.Blackboard.ImmediateThreats)
            {
                Ship threatShip = threat.Key;

                if (closestShip == null)
                {
                    closestShip = threatShip;
                }
                else
                {
                    if (Vector2.Distance(threatShip.Controller.ShipTransform.position, ship.Controller.ShipTransform.position) < Vector3.Distance(closestShip.Controller.ShipTransform.position, ship.Controller.ShipTransform.position))
                    {
                        closestShip = threatShip;
                    }
                }
            }

            return closestShip;
        }

        private Ship FindBiggestThreat()
        {
            KeyValuePair<Ship, int> biggestThreat = new KeyValuePair<Ship, int>();

            if (ship.AICore.Blackboard.ImmediateThreats.Count == 0)
            {
                return null;
            }

            foreach (var threat in ship.AICore.Blackboard.ImmediateThreats)
            {
                Ship threatShip = threat.Key;
                int threatDamage = threat.Value;

                if (biggestThreat.Key == null)
                {
                    biggestThreat = threat;
                }
                else
                {
                    if (threatDamage > biggestThreat.Value)
                    {
                        biggestThreat = threat;
                    }
                }
            }

            return biggestThreat.Key;
        }
    }
}