using UnityEngine;

namespace Starfire
{
    public enum CommandStatus
    {
        None,
        InProgress,
        Completed
    }

    public abstract class Command<T> : ICommand
    {
        protected Ship ship;
        protected T target;
        protected CommandStatus commandStatus;

        public Command(Ship _ship, T _target)
        {
            ship = _ship;
            target = _target;
            commandStatus = CommandStatus.InProgress;
        }

        public CommandStatus GetCommandStatus() => commandStatus;

        public Vector2 GetTargetPosition()
        {
            switch (target)
            {
                case Ship _ship:
                    return _ship.Controller.ShipTransform.position;
                case Vector2 _vector2:
                    return _vector2;
                case Transform _transform:
                    return _transform.position;
                default:
                    return Vector2.zero;
            }
        }

        public abstract void Enter();
        public abstract void Execute();
        public abstract void Exit();
    }
}