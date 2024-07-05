using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Mathematics;
using UnityEngine;

namespace Starfire
{
    public abstract class AICore
    {
        protected Ship ship;
        protected Fleet fleet;

        protected CommandStateMachine commandStateMachine;

        public AICore(Ship _ship, Fleet _fleet = default)
        {
            ship = _ship;
            commandStateMachine = new CommandStateMachine();
            commandStateMachine.ChangeState(new CommandIdle());

            if (_fleet != default)
            {
                fleet = _fleet;
            
            }
            fleet = _fleet;


        }

        public virtual void Update()
        {
            commandStateMachine.Update();
        }
    }
}