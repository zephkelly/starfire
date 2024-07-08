using UnityEngine;

namespace Starfire
{
    public abstract class Command<T> : ICommand
    {
        protected T target;

        public Command(T _target)
        {
            target = _target;
        }

        public Vector2 GetTargetPosition()
        {
            switch (target)
            {
                case Ship ship:
                    return ship.Controller.ShipTransform.position;
                case Vector2 vector2:
                    return vector2;
                case Transform transform:
                    return transform.position;
                default:
                    return Vector2.zero;
            }
        }

        public abstract void Enter();
        public abstract void Execute();
        public abstract void Exit();
    }
}