using System.Numerics;

namespace Starfire
{
    public abstract class AICore
    {
        protected Ship ship;
        protected Fleet fleet;

        protected CommandStateMachine commandStateMachine;

        public CommandStatus GetCommandStatus() => commandStateMachine.GetCommandStatus();

        public AICore() { }

        public void SetShip(Ship _ship, Fleet _fleet = default)
        {
            ship = _ship;
            commandStateMachine = new CommandStateMachine();

            if (_fleet != default)
            {
                fleet = _fleet;
            }
        }

        public virtual void SetCurrentCommand(ICommand newCommand)
        {
            commandStateMachine.ChangeState(newCommand);
        }

        public virtual void Update()
        {
            commandStateMachine.Update();
        }
    }
}