namespace Starfire
{
    public class CheckForImmediateThreats : Node
    {
        private Ship ship;

        public CheckForImmediateThreats(Ship ship)
        {
            this.ship = ship;
        }

        public override void Initialise() { }

        public override NodeState Evaluate()
        {
            if (ship.AICore.Blackboard.DetectedThreats.Count > 0)
            {
                state = NodeState.Success;
            }
            else
            {
                state = NodeState.Failure;
            }
            
            return state;
        }

        public override void FixedEvaluate() { }

        public override void Terminate() { }
    }
}