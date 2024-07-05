namespace Starfire
{
    public interface IFleetState
    {
        void Enter();
        void Execute();
        void Exit();
    }

    public class FleetStateMachine
    {
        public IFleetState CurrentFleetState { get; private set; }

        public void ChangeState(IFleetState newState)
        {
            if(CurrentFleetState != null)
            {
                CurrentFleetState.Exit();
            }
            
            CurrentFleetState = newState;
            CurrentFleetState.Enter();
        }

        public void Update()
        {
            if(CurrentFleetState != null)
            {
                CurrentFleetState.Execute();
            }
        }
    }
}