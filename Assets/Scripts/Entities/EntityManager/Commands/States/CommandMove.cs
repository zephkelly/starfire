using UnityEngine;

namespace Starfire
{
    public class CommandMove<T> : Command<T>
    {
        public CommandMove(Ship _ship, T _target) : base(_ship, _target) { }

        public override void Enter()
        {

        }

        public override void Execute()
        {
            if (Vector2.Distance(ship.Controller.ShipTransform.position, GetTargetPosition()) < 20f)
            {
                if (commandStatus is CommandStatus.Completed) return;
                commandStatus = CommandStatus.Completed;
            }
        }

        public override void Exit() { }
    }
}