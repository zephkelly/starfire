namespace Starfire
{
    public interface ICommand
    {
        CommandStatus GetCommandStatus();
        void Enter();
        void Execute();
        void Exit();
    }
}