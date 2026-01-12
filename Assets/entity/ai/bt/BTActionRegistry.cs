using System;
using System.Collections.Generic;
using Starfire.Entity.Modules;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Registry of action types that can be created from parameters.
    /// Add new actions here to make them available in the behavior tree editor.
    /// </summary>
    public static class BTActionRegistry
    {
        /// <summary>
        /// Factory delegate for creating action nodes.
        /// </summary>
        public delegate IBTNode ActionFactory(IBTNodeParameters parameters);

        /// <summary>
        /// Info about a registered action type.
        /// </summary>
        public class ActionInfo
        {
            public string Name { get; }
            public Type ParameterType { get; }
            public ActionFactory Factory { get; }

            public ActionInfo(string name, Type parameterType, ActionFactory factory)
            {
                Name = name;
                ParameterType = parameterType;
                Factory = factory;
            }
        }

        private static readonly Dictionary<string, ActionInfo> _actions = new();

        static BTActionRegistry()
        {
            // Target Setting
            Register("SetTargetPosition", typeof(SetTargetPositionParameters), CreateSetTargetPositionAction);
            Register("SetTargetFromEntity", typeof(SetTargetFromEntityParameters), CreateSetTargetFromEntityAction);
            Register("SetTargetFromWaypoints", typeof(SetTargetFromWaypointsParameters), CreateSetTargetFromWaypointsAction);
            Register("SetTargetFromWaypointsDynamic", typeof(SetTargetFromWaypointsDynamicParameters), CreateSetTargetFromWaypointsDynamicAction);

            // Steering Calculation
            Register("CalculateSeek", typeof(CalculateSeekParameters), CreateCalculateSeekAction);
            Register("CalculateArrive", typeof(CalculateArriveParameters), CreateCalculateArriveAction);
            Register("CalculateFlee", typeof(CalculateFleeParameters), CreateCalculateFleeAction);

            // Steering Application
            Register("ApplySteering", typeof(ApplySteeringParameters), CreateApplySteeringAction);

            // Rotation
            Register("RotateTowardTarget", typeof(RotateTowardTargetParameters), CreateRotateTowardTargetAction);
            Register("RotateTowardVelocity", typeof(RotateTowardVelocityParameters), CreateRotateTowardVelocityAction);

            // Waypoint Management
            Register("AdvanceWaypointIndex", typeof(AdvanceWaypointIndexParameters), CreateAdvanceWaypointIndexAction);
            Register("InitWaypointStack", typeof(InitWaypointStackParameters), CreateInitWaypointStackAction);
            Register("GenerateRandomSubWaypoints", typeof(GenerateRandomSubWaypointsParameters), CreateGenerateRandomSubWaypointsAction);
            Register("PushWaypoints", typeof(PushWaypointsParameters), CreatePushWaypointsAction);
            Register("PopWaypoints", typeof(PopWaypointsParameters), CreatePopWaypointsAction);

            // Conditions - Waypoint Stack
            Register("IsStackDepth", typeof(IsStackDepthParameters), CreateIsStackDepthCondition);
            Register("IsSequenceComplete", typeof(IsSequenceCompleteParameters), CreateIsSequenceCompleteCondition);

            // Conditions - Position
            Register("IsNearPosition", typeof(IsNearPositionParameters), CreateIsNearPositionCondition);
            Register("IsStopped", typeof(IsStoppedParameters), CreateIsStoppedCondition);

            // Conditions - Module
            Register("HasModuleType", typeof(HasModuleTypeParameters), CreateHasModuleTypeCondition);
            Register("HasModuleCategory", typeof(HasModuleCategoryParameters), CreateHasModuleCategoryCondition);
            Register("HasModuleSubCategory", typeof(HasModuleSubCategoryParameters), CreateHasModuleSubCategoryCondition);

            // Conditions - Target
            Register("HasTarget", typeof(HasTargetParameters), CreateHasTargetCondition);
            Register("IsInRange", typeof(IsInRangeParameters), CreateIsInRangeCondition);

            // Trajectory Prediction
            Register("CalculateTrajectoryPrediction", typeof(CalculateTrajectoryPredictionParameters), CreateCalculateTrajectoryPredictionAction);

            // Smart Steering
            Register("CalculateSmartArrive", typeof(CalculateSmartArriveParameters), CreateCalculateSmartArriveAction);
            Register("CalculateRecovery", typeof(CalculateRecoveryParameters), CreateCalculateRecoveryAction);
            Register("CalculateFlyThrough", typeof(CalculateFlyThroughParameters), CreateCalculateFlyThroughAction);

            // Trajectory Conditions
            Register("WillMissTarget", typeof(WillMissTargetParameters), CreateWillMissTargetCondition);
            Register("HasOvershot", typeof(HasOvershotParameters), CreateHasOvershotCondition);

            // Cruise Speed Control
            Register("SetCruiseSpeed", typeof(SetCruiseSpeedParameters), CreateSetCruiseSpeedAction);
            Register("SetDynamicCruiseSpeed", typeof(SetDynamicCruiseSpeedParameters), CreateSetDynamicCruiseSpeedAction);

            // Utility Actions
            Register("Wait", typeof(WaitParameters), CreateWaitAction);

            // Random Conditions
            Register("RandomChance", typeof(RandomChanceParameters), CreateRandomChanceCondition);

            // High-Angle Redirect
            Register("IsHighApproachAngle", typeof(IsHighApproachAngleParameters), CreateIsHighApproachAngleCondition);
            Register("CalculateBrakeAndTurn", typeof(CalculateBrakeAndTurnParameters), CreateCalculateBrakeAndTurnAction);
        }

        /// <summary>
        /// Register a new action type.
        /// </summary>
        public static void Register(string actionType, Type parameterType, ActionFactory factory)
        {
            _actions[actionType] = new ActionInfo(actionType, parameterType, factory);
        }

        /// <summary>
        /// Create an action instance from its type and parameters.
        /// </summary>
        public static IBTNode CreateAction(string actionType, IBTNodeParameters parameters)
        {
            if (!_actions.TryGetValue(actionType, out var info))
            {
                throw new ArgumentException($"Unknown action type: {actionType}");
            }

            return info.Factory(parameters);
        }

        /// <summary>
        /// Get all registered action types.
        /// </summary>
        public static IEnumerable<string> GetActionTypes()
        {
            return _actions.Keys;
        }

        /// <summary>
        /// Get info about a specific action type.
        /// </summary>
        public static ActionInfo GetActionInfo(string actionType)
        {
            return _actions.TryGetValue(actionType, out var info) ? info : null;
        }

        /// <summary>
        /// Create default parameters for an action type.
        /// </summary>
        public static IBTNodeParameters CreateDefaultParameters(string actionType)
        {
            var info = GetActionInfo(actionType);
            if (info == null) return null;

            return (IBTNodeParameters)Activator.CreateInstance(info.ParameterType);
        }

        // Factory methods for target setting actions

        private static IBTNode CreateSetTargetPositionAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetTargetPositionParameters ?? new SetTargetPositionParameters();
            return new SetTargetPositionAction(p.position, p.targetKey);
        }

        private static IBTNode CreateSetTargetFromEntityAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetTargetFromEntityParameters ?? new SetTargetFromEntityParameters();
            return new SetTargetFromEntityAction(p.entityKey, p.targetKey);
        }

        private static IBTNode CreateSetTargetFromWaypointsAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetTargetFromWaypointsParameters ?? new SetTargetFromWaypointsParameters();
            return new SetTargetFromWaypointsAction(p.waypointsKey, p.indexKey, p.targetKey, p.stackKey);
        }

        private static IBTNode CreateSetTargetFromWaypointsDynamicAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetTargetFromWaypointsDynamicParameters ?? new SetTargetFromWaypointsDynamicParameters();
            return new SetTargetFromWaypointsDynamicAction(p.waypointsKey, p.indexKey, p.targetKey);
        }

        // Factory methods for steering calculation actions

        private static IBTNode CreateCalculateSeekAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateSeekParameters ?? new CalculateSeekParameters();
            return new CalculateSeekAction(p.targetKey, p.outputKey);
        }

        private static IBTNode CreateCalculateArriveAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateArriveParameters ?? new CalculateArriveParameters();
            return new CalculateArriveAction(p.targetKey, p.outputKey, p.arrivalThreshold);
        }

        private static IBTNode CreateCalculateFleeAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateFleeParameters ?? new CalculateFleeParameters();
            return new CalculateFleeAction(p.targetKey, p.outputKey);
        }

        // Factory method for steering application

        private static IBTNode CreateApplySteeringAction(IBTNodeParameters parameters)
        {
            var p = parameters as ApplySteeringParameters ?? new ApplySteeringParameters();
            return new ApplySteeringAction(p.forceKey);
        }

        // Factory methods for rotation actions

        private static IBTNode CreateRotateTowardTargetAction(IBTNodeParameters parameters)
        {
            var p = parameters as RotateTowardTargetParameters ?? new RotateTowardTargetParameters();
            return new RotateTowardTargetAction(p.targetKey);
        }

        private static IBTNode CreateRotateTowardVelocityAction(IBTNodeParameters parameters)
        {
            var p = parameters as RotateTowardVelocityParameters ?? new RotateTowardVelocityParameters();
            return new RotateTowardVelocityAction(p.minSpeedThreshold);
        }

        // Factory methods for waypoint management

        private static IBTNode CreateAdvanceWaypointIndexAction(IBTNodeParameters parameters)
        {
            var p = parameters as AdvanceWaypointIndexParameters ?? new AdvanceWaypointIndexParameters();
            return new AdvanceWaypointIndexAction(p.waypointsKey, p.indexKey, p.mode, p.directionKey, p.stackKey, p.transformsKey);
        }

        private static IBTNode CreateInitWaypointStackAction(IBTNodeParameters parameters)
        {
            var p = parameters as InitWaypointStackParameters ?? new InitWaypointStackParameters();
            return new InitWaypointStackAction(p.waypointsKey, p.stackKey, p.mode, p.maxDepth, p.transformsKey);
        }

        private static IBTNode CreateGenerateRandomSubWaypointsAction(IBTNodeParameters parameters)
        {
            var p = parameters as GenerateRandomSubWaypointsParameters ?? new GenerateRandomSubWaypointsParameters();
            return new GenerateRandomSubWaypointsAction(
                p.centerKey,
                p.outputKey,
                p.count,
                p.minRadius,
                p.maxRadius,
                p.avoidCenter);
        }

        private static IBTNode CreatePushWaypointsAction(IBTNodeParameters parameters)
        {
            var p = parameters as PushWaypointsParameters ?? new PushWaypointsParameters();
            return new PushWaypointsAction(p.stackKey, p.waypointsKey, p.mode, p.label);
        }

        private static IBTNode CreatePopWaypointsAction(IBTNodeParameters parameters)
        {
            var p = parameters as PopWaypointsParameters ?? new PopWaypointsParameters();
            return new PopWaypointsAction(p.stackKey);
        }

        private static IBTNode CreateIsStackDepthCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsStackDepthParameters ?? new IsStackDepthParameters();
            return new IsStackDepthCondition(p.stackKey, p.depth, p.comparison);
        }

        private static IBTNode CreateIsSequenceCompleteCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsSequenceCompleteParameters ?? new IsSequenceCompleteParameters();
            return new IsSequenceCompleteCondition(p.stackKey);
        }

        // Factory methods for conditions

        private static IBTNode CreateIsNearPositionCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsNearPositionParameters ?? new IsNearPositionParameters();
            return new IsNearPositionCondition(p.targetKey, p.threshold);
        }

        private static IBTNode CreateIsStoppedCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsStoppedParameters ?? new IsStoppedParameters();
            return new IsStoppedCondition(p.threshold);
        }


        private static IBTNode CreateHasTargetCondition(IBTNodeParameters parameters)
        {
            var p = parameters as HasTargetParameters ?? new HasTargetParameters();
            return new HasTargetCondition(p.targetKey);
        }

        private static IBTNode CreateIsInRangeCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsInRangeParameters ?? new IsInRangeParameters();
            return new IsInRangeCondition(p.targetKey, p.minRange, p.maxRange);
        }

        // Factory methods for trajectory prediction

        private static IBTNode CreateCalculateTrajectoryPredictionAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateTrajectoryPredictionParameters ?? new CalculateTrajectoryPredictionParameters();
            return new CalculateTrajectoryPredictionAction(p.targetKey, p.predictionKey, p.missThreshold);
        }

        // Factory methods for smart steering

        private static IBTNode CreateCalculateSmartArriveAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateSmartArriveParameters ?? new CalculateSmartArriveParameters();
            return new CalculateSmartArriveAction(
                p.targetKey,
                p.predictionKey,
                p.outputKey,
                p.arrivalThreshold,
                p.angleStiffness,
                p.lateralBrakingFactor,
                p.closeRangeThreshold,
                p.alignmentAngle);
        }

        private static IBTNode CreateCalculateRecoveryAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateRecoveryParameters ?? new CalculateRecoveryParameters();
            return new CalculateRecoveryAction(
                p.targetKey,
                p.predictionKey,
                p.outputKey,
                p.speedThreshold,
                p.turnBrakeFactor);
        }

        private static IBTNode CreateCalculateFlyThroughAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateFlyThroughParameters ?? new CalculateFlyThroughParameters();
            return new CalculateFlyThroughAction(
                p.targetKey,
                p.predictionKey,
                p.outputKey,
                p.passRadius);
        }

        // Factory methods for trajectory conditions

        private static IBTNode CreateWillMissTargetCondition(IBTNodeParameters parameters)
        {
            var p = parameters as WillMissTargetParameters ?? new WillMissTargetParameters();
            return new WillMissTargetCondition(p.predictionKey, p.missThreshold);
        }

        private static IBTNode CreateHasOvershotCondition(IBTNodeParameters parameters)
        {
            var p = parameters as HasOvershotParameters ?? new HasOvershotParameters();
            return new HasOvershotCondition(p.predictionKey, p.minDistance);
        }

        // Factory methods for cruise speed control

        private static IBTNode CreateSetCruiseSpeedAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetCruiseSpeedParameters ?? new SetCruiseSpeedParameters();
            return new SetCruiseSpeedAction(p.speed, p.acceleration, p.speedKey, p.accelKey);
        }

        private static IBTNode CreateSetDynamicCruiseSpeedAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetDynamicCruiseSpeedParameters ?? new SetDynamicCruiseSpeedParameters();
            return new SetDynamicCruiseSpeedAction(
                p.minSpeedPercent,
                p.maxSpeedPercent,
                p.minDistance,
                p.maxDistance,
                p.segmentInfluence,
                p.scaleAcceleration,
                p.minAccelPercent,
                p.maxAccelPercent,
                p.targetKey,
                p.stackKey,
                p.speedKey,
                p.accelKey);
        }

        // Factory methods for utility actions

        private static IBTNode CreateWaitAction(IBTNodeParameters parameters)
        {
            var p = parameters as WaitParameters ?? new WaitParameters();
            return new WaitAction(p.duration, p.elapsedKey);
        }

        // Factory methods for random conditions

        private static IBTNode CreateRandomChanceCondition(IBTNodeParameters parameters)
        {
            var p = parameters as RandomChanceParameters ?? new RandomChanceParameters();
            return new RandomChanceCondition(p.chance, p.evaluateOnce, p.resultKey, p.resetTriggerKey);
        }

        // Factory methods for high-angle redirect

        private static IBTNode CreateIsHighApproachAngleCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsHighApproachAngleParameters ?? new IsHighApproachAngleParameters();
            return new IsHighApproachAngleCondition(p.predictionKey, p.angleThreshold);
        }

        private static IBTNode CreateCalculateBrakeAndTurnAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateBrakeAndTurnParameters ?? new CalculateBrakeAndTurnParameters();
            return new CalculateBrakeAndTurnAction(p.targetKey, p.outputKey, p.brakeFactor, p.minSpeedThreshold);
        }

        // Factory methods for hierarchical module conditions

        private static IBTNode CreateHasModuleTypeCondition(IBTNodeParameters parameters)
        {
            var p = parameters as HasModuleTypeParameters ?? new HasModuleTypeParameters();
            return new HasModuleTypeCondition(p.moduleType);
        }

        private static IBTNode CreateHasModuleCategoryCondition(IBTNodeParameters parameters)
        {
            var p = parameters as HasModuleCategoryParameters ?? new HasModuleCategoryParameters();
            return new HasModuleCategoryCondition(p.category, p.minimumCount);
        }

        private static IBTNode CreateHasModuleSubCategoryCondition(IBTNodeParameters parameters)
        {
            var p = parameters as HasModuleSubCategoryParameters ?? new HasModuleSubCategoryParameters();
            return new HasModuleSubCategoryCondition(p.subCategory, p.minimumCount);
        }
    }
}
