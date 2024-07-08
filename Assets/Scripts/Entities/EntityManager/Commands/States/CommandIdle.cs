using UnityEngine;

namespace Starfire
{
    public class CommandIdle<T> : Command<T>
    {
        public CommandIdle(Ship _ship, T _target) : base(_ship, _target) { }

        public override void Enter()
        {
            commandStatus = CommandStatus.Completed;
        }

        public override void Execute() { }

        public override void Exit() { }
    }
}