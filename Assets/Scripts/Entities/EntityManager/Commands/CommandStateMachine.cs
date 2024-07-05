namespace Starfire
{
    public interface ICommand
    {
        void Enter();
        void Execute();
        void Exit();
    }

    public class CommandStateMachine
    {
        public ICommand CurrentCommand { get; private set; }

        public void ChangeState(ICommand newCommand)
        {
            if(CurrentCommand != null)
            {
                CurrentCommand.Exit();
            }
            
            CurrentCommand = newCommand;
            CurrentCommand.Enter();
        }

        public void Update()
        {
            if(CurrentCommand != null)
            {
                CurrentCommand.Execute();
            }
        }
    }
}