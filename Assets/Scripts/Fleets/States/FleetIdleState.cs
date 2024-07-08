namespace Starfire
{
    public class FleetIdleState : IState
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

        public void FixedUpdate()
        {
            // Debug.Log("Fleet Idle State Fixed Executed");
        }

        public void Exit()
        {
            // Debug.Log("Fleet Idle State Exited");
        }
    }
}