using UnityEngine;

namespace Starfire
{
    // Root (Selector)
    // |-- Emergency Response (Sequence)
    //     |-- Check For Immediate Threats
    //     |-- Respond To Threats (Selector)
    //         |-- Perform Evasive Maneuvers
    //         |-- Counter-Attack (Sequence)
    //             |-- Choose Target (store in blackboard)
    //             |-- GetDirectionToTarget (store in blackboard)
    //             |-- Engage Target (Selector)
    //                 |-- Close Range Engagement (Sequence)
    //                     |-- Check If Close Range
    //                     |-- AddCircleBiasToHeading
    //                     |-- Move Towards Heading
    //                 |-- Medium Range Engagement (Sequence)
    //                     |-- Check If Medium Range
    //                     |-- Move Towards Target Direction
    //                 |-- Long Range Engagement (Sequence)
    //                     |-- Check If Long Range
    //                     |-- Warp Towards Target Direction
    //             |-- Combat Sequence (Sequence)
    //                 |-- Get Fire Direction (store in blackboard)
    //                 |-- Weapon Firing (Sequence)
    //                     |-- Check If Can Fire (FOV and range check)
    //                     |-- Choose Appropriate Weapon
    //                     |-- Apply Weapon-Specific Bias to Fire Direction
    //                     |-- Fire Weapon
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
            var moveToTargetNode = new SimpleMoveToTarget(ship);
            var chooseTargetNode = new ChooseTarget(ship);
            var checkForImmediateThreatsNode = new CheckForImmediateThreats(ship);
            var evasiveManeuversNode = new EvasiveManeuvers(ship);

            var rootSelector = new SelectorNode();
            var emergencyResponseSequence = new SequenceNode();
            var respondToThreatsSelector = new SelectorNode();
            var counterAttackSequence = new SequenceNode();
            var engageTargetSelector = new SelectorNode();
            var closeRangeEngagementSequence = new SequenceNode();

            closeRangeEngagementSequence.AddNode(new CheckIfCloseRange(ship));
            closeRangeEngagementSequence.AddNode(new AddCircleBiasToHeading(ship));
            closeRangeEngagementSequence.AddNode(new MoveToHeading(ship));

            engageTargetSelector.AddNode(closeRangeEngagementSequence);

            counterAttackSequence.AddNode(chooseTargetNode);
            counterAttackSequence.AddNode(new GetHeadingToTarget(ship));
            counterAttackSequence.AddNode(engageTargetSelector);

            respondToThreatsSelector.AddNode(evasiveManeuversNode);
            respondToThreatsSelector.AddNode(counterAttackSequence);

            emergencyResponseSequence.AddNode(checkForImmediateThreatsNode);
            emergencyResponseSequence.AddNode(respondToThreatsSelector);

            rootSelector.AddNode(emergencyResponseSequence);
            rootSelector.AddNode(moveToTargetNode);

            behaviourTree = new BehaviourTree(rootSelector);  
        }

        public void SetTarget(Ship newTargetShip)
        {
            blackboard.SetCurrentTarget(newTargetShip);
        }
        public void SetTarget(Vector2 newTargetVector)
        {
            blackboard.SetCurrentTarget(newTargetVector);
        }

        public void SetTarget(Transform newTargetTransform)
        {
            blackboard.SetCurrentTarget(newTargetTransform);
        }

        public virtual void Update()
        {
            if (behaviourTree == null) return;
            behaviourTree.Evaluate();
        }

        public abstract Vector2 CalculateAvoidanceSteeringDirection(GameObject ourShipObject, Vector2 ourShipPosition, float ourShipVelocityMagnitude, Vector2 currentDirection, LayerMask whichRaycastableLayers, int numberOfRays, float collisionCheckRadius = 30f);

        public abstract Vector2 AddCircleTargetBias(Vector2 weightedDirection, Vector2 ourShipPosition, Vector2 ourShipVelocity, Vector2 targetShipPosition, float orbitDistance, int orbitDirection);
    }
}