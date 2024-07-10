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

        protected override NodeState OnEvaluate()
        {
            if (ShouldEvade())
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

        private bool ShouldEvade()
        {
            return false;
        }
    }
}