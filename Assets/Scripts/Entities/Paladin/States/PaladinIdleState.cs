using UnityEngine;

namespace Starfire
{
    public class PaladinIdleState : IState
    {
        private  PaladinShipController _shipController;

        public PaladinIdleState(PaladinShipController controller)
        {
            _shipController = controller;
        }

        public void Enter()
        {
            _shipController.SetThrusters(false, Vector2.zero, false);
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
