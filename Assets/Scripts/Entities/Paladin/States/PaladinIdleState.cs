using UnityEngine;

namespace Starfire
{
    public class PaladinIdleState : IState
    {
        private  PaladinShipController shipController;

        public PaladinIdleState(PaladinShipController _paladin)
        {
            shipController = _paladin;
        }

        public void Enter()
        {
            
        }

        public void Execute()
        {

        }

        public void FixedUpdate()
        {

        }

        public void Exit()
        {

        }
    }
}
