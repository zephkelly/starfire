using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public class EvasiveManeuvers : Node
    {
        private Ship ship;

        public EvasiveManeuvers(Ship ship)
        {
            this.ship = ship;
        }

        public override void Initialise() { }

        public override NodeState Evaluate()
        {
            var currentThreats = ship.AICore.Blackboard.DetectedThreats;

            if (ShouldEvade(currentThreats))
            {
                PerformEvasiveManeuvers();

                if (true)
                {
                    state = NodeState.Success;
                }
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }

        private void PerformEvasiveManeuvers()
        {
            Debug.Log("Performing evasive maneuvers");
        }

        public override void FixedEvaluate() { }

        private bool ShouldEvade(IReadOnlyList<Ship> currentThreats)
        {
            return false;
            // return ship.Configuration.Health < ship.Configuration.MaxHealth * 0.5f;
        }

        public override void Terminate() { }
    }
}