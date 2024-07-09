using UnityEngine;

namespace Starfire
{
    public class CounterAttack : Node
    {
        private Ship ship;

        public CounterAttack(Ship ship)
        {
            this.ship = ship;
        }

        public override void Initialise() { }

        public override NodeState Evaluate()
        {
            var currentThreats = ship.AICore.Blackboard.DetectedThreats;

            if (CanCounterAttack())
            {
                PerformCounterAttack();

                state = NodeState.Running;
            }
            else
            {
                state = NodeState.Failure;
            }

            return state;
        }

        public override void FixedEvaluate() { }

        private void PerformCounterAttack()
        {
            Debug.Log("Counter-attacking");
        }

        private bool CanCounterAttack()
        {
            // Implement logic to decide if counter-attack is possible
            // return ship.Health > 50 && ship.HasAmmo() && !ship.IsOutgunned();
            return true;
        }

        public override void Terminate() { }
    }
}