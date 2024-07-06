using UnityEngine;

namespace Starfire
{
    public class CommandIdle<T> : Command<T>
    {
        public CommandIdle(T _target) : base(_target) { }

        public override void Enter()
        {
            Debug.Log("CommandIdle: Enter");
        }

        public override void Execute()
        {
            Debug.Log("CommandIdle: Execute");
        }

        public override void Exit()
        {
            Debug.Log("CommandIdle: Exit");
        }
    }
}