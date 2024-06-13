using UnityEngine;

namespace Starfire
{
    public class ScavengerIdleState : IState
    {
        private  ScavengerShipController shipController;

        public ScavengerIdleState(ScavengerShipController _scavenger)
        {
            shipController = _scavenger;
        }

        public void Enter()
        {
            Debug.Log("Scavenger is idle!");
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
