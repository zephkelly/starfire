using System.Numerics;

namespace Starfire
{
    public abstract class AICore
    {
        protected BehaviourTree behaviourTree;

        protected Ship ship;
        protected Fleet fleet;

        public AICore() { }

        public void SetShip(Ship _ship, Fleet _fleet = default)
        {
            ship = _ship;

            if (_fleet != default)
            {
                fleet = _fleet;
            }

            // Set up the behaviour tree
            behaviourTree = new BehaviourTree();
        }

        public virtual void Update()
        {
            behaviourTree.Evaluate();
        }
    }
}