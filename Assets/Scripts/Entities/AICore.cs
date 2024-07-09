using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
    public abstract class AICore
    {
        protected Blackboard blackboard;
        protected BehaviourTree behaviourTree;
        protected SelectorNode rootNode;
        protected MoveToTargetNode moveToTargetNode;

        protected Ship ship;
        protected Fleet fleet;

        public AICore()
        {
            blackboard = new Blackboard();
        }

        public void SetShip(Ship _ship)
        {
            ship = _ship;
        }

        public void SetFleet(Fleet _fleet)
        {
            fleet = _fleet;
        }
        public void SetBlackboard(Blackboard _blackboard)
        {
            if (_blackboard == null) return;
            blackboard.SetFleetBlackboard(_blackboard);
        }

        public void CreateBehaviourTree()
        {
            Debug.Log(blackboard.GetValue("Fleet"));
            Debug.Log(blackboard.FleetBlackboard.GetValue("Fleet"));

            rootNode = new SelectorNode(blackboard);
            moveToTargetNode = new MoveToTargetNode(ship, blackboard);

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
            if (behaviourTree == null) return;
            behaviourTree.Evaluate();
        }

        public virtual void FixedUpdate()
        {
            if (behaviourTree == null) return;
            behaviourTree.FixedEvaluate();
        }
    }
}