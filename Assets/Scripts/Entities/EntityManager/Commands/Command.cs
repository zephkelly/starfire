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

        public abstract void Enter();
        public abstract void Execute();
        public abstract void Exit();
    }
}