namespace Starfire
{
    public interface ICommand
    {
        void Enter();
        void Execute();
        void Exit();
    }
}