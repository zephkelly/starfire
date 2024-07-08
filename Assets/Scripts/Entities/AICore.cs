using System.Numerics;

namespace Starfire
{
    public abstract class AICore
    {
        protected Ship ship;
        protected Fleet fleet;

        protected CommandStateMachine commandStateMachine;

        public AICore() { }

        public virtual void SetShip(Ship _ship, Fleet _fleet = default)
        {
            ship = _ship;
            commandStateMachine = new CommandStateMachine();
            commandStateMachine.ChangeState(new CommandIdle<Vector2>(Vector2.Zero));

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