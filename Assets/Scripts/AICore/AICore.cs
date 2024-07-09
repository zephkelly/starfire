using UnityEngine;

namespace Starfire
{
    // Root (Selector)
    // |-- Emergency Response (Sequence)
    //     |-- Check For Immediate Threats
    //     |-- Respond To Threats (Selector)
    //         |-- Perform Evasive Maneuvers
    //         |-- Counter-Attack
    // |-- Follow Fleet Command (Sequence)
    //     |-- Get Fleet Command
    //     |-- Execute Fleet Command (Selector)
    //         |-- Move To Target (Sequence)
    //             |-- Check If In Star's Orbit (Decorator)
    //             |-- Move To Target Position
    //             |-- Avoid Gravity (if needed)
    //         |-- Orbit Star (Sequence)
    //             |-- Check If Outside Star's Orbit (Decorator)
    //             |-- Move To Orbit
    //             |-- Maintain Orbit
    // |-- Individual Ship Behavior (Sequence)
    //     |-- Patrol Area
    //     |-- Perform Routine Tasks


    public abstract class AICore : IAICore
    {
        protected Blackboard blackboard;
        protected BehaviourTree behaviourTree;

        protected MoveToTargetNode moveToTargetNode;

        protected Ship ship;
        protected Fleet fleet;

        public Blackboard Blackboard => blackboard;

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

        public void SetFleetBlackboard(FleetBlackboard _blackboard)
        {
            if (_blackboard == null) return;
            blackboard.SetFleetBlackboard(_blackboard);
        }

        public void CreateBehaviourTree()
        {
            var root = new SelectorNode();
            
            var respondToThreats = new RespondToThreats(ship);
            respondToThreats.AddNode(new EvasiveManeuvers(ship));
            respondToThreats.AddNode(new CounterAttack(ship));

            var emergencyResponse = new EmergencyResponse(ship);
            emergencyResponse.AddNode(new CheckForImmediateThreats(ship));
            emergencyResponse.AddNode(respondToThreats);

            moveToTargetNode = new MoveToTargetNode(ship);

            root.AddNode(emergencyResponse);
            root.AddNode(moveToTargetNode);

            behaviourTree = new BehaviourTree(root);
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

        public virtual Vector2 CalculateAvoidanceSteeringDirection(GameObject ourShipObject, Vector2 ourShipPosition, float ourShipVelocityMagnitude, Vector2 currentDirection, LayerMask whichRaycastableLayers, int numberOfRays, float collisionCheckRadius = 30f)
        {
            return Vector2.zero;
        }
    }
}