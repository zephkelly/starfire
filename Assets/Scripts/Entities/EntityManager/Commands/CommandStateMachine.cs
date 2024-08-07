namespace Starfire
{
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

        public CommandStatus GetCommandStatus()
        {
            if(CurrentCommand != null)
            {
                return CurrentCommand.GetCommandStatus();
            }
            else
            {
                return CommandStatus.None;
            }
        }
    }
}