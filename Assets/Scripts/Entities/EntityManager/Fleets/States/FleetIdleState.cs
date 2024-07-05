namespace Starfire
{
    public class FleetIdleState : IFleetState
    {
        private Fleet fleet;

        public FleetIdleState(Fleet _fleet)
        {
            fleet = _fleet;
        }

        public void Enter()
        {
            // Debug.Log("Fleet Idle State Entered");
        }

        public void Execute()
        {
            // Debug.Log("Fleet Idle State Executed");
        }

        public void Exit()
        {
            // Debug.Log("Fleet Idle State Exited");
        }
    }
}