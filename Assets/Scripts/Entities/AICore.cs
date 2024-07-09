using UnityEngine;

namespace Starfire
{
    public abstract class AICore
    {
        protected BehaviourTree behaviourTree;
        private MoveToTargetNode moveToTargetNode;

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

            var rootNode = new SelectorNode();

            moveToTargetNode = new MoveToTargetNode(ship);
            rootNode.AddNode(moveToTargetNode);

            behaviourTree = new BehaviourTree(rootNode);
        }

        public void SetTarget(Ship _target)
        {
            moveToTargetNode.SetTarget(_target);
        }
        public void SetTarget(Vector2 _target)
        {
            moveToTargetNode.SetTarget(_target);
        }

        public void SetTarget(Transform _target)
        {
            moveToTargetNode.SetTarget(_target);
        }

        public virtual void Update()
        {
            behaviourTree.Evaluate();
        }

        public virtual void FixedUpdate()
        {
            behaviourTree.FixedUpdate();
        }
    }
}