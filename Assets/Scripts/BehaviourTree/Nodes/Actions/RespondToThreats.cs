namespace Starfire
{
    public class RespondToThreats : SelectorNode
    {
        private Ship ship;

        public RespondToThreats(Ship ship)
        {
            this.ship = ship;
            nodes.Add(new EvasiveManeuvers(ship));
            nodes.Add(new CounterAttack(ship));
        }

        public override NodeState Evaluate()
        {
            NodeState childState = base.Evaluate();
            
            if (childState == NodeState.Running)
            {
                state = NodeState.Running;
            }
            else if (ship.AICore.Blackboard.DetectedThreats.Count == 0)
            {
                state = NodeState.Success;
            }
            else
            {
                state = NodeState.Failure;
            }
            return state;
        }
    }
}