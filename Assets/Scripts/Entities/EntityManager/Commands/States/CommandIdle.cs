namespace Starfire
{
    public class CommandIdle : ICommand
    {
        public CommandIdle()
        {
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