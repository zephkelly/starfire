using System;
using System.Collections.Generic;
using System.Linq;
using Starfire.Entity.Modules;
using Starfire.Entity.Modules.Sensor;

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
            public string EntityType { get; }
            public string Category { get; }

            public ActionInfo(string name, Type parameterType, ActionFactory factory, string entityType, string category)
            {
                Name = name;
                ParameterType = parameterType;
                Factory = factory;
                EntityType = entityType;
                Category = category;
            }
        }

        private static readonly Dictionary<string, ActionInfo> _actions = new();

        static BTActionRegistry()
        {
            // Ship > Target Setting
            Register("SetTargetPosition", typeof(SetTargetPositionParameters), CreateSetTargetPositionAction, "Ship", "Target");
            Register("SetTargetFromEntity", typeof(SetTargetFromEntityParameters), CreateSetTargetFromEntityAction, "Ship", "Target");
            Register("SetTargetFromWaypoints", typeof(SetTargetFromWaypointsParameters), CreateSetTargetFromWaypointsAction, "Ship", "Target");
            Register("SetTargetFromWaypointsDynamic", typeof(SetTargetFromWaypointsDynamicParameters), CreateSetTargetFromWaypointsDynamicAction, "Ship", "Target");

            // Ship > Steering
            Register("CalculateSeek", typeof(CalculateSeekParameters), CreateCalculateSeekAction, "Ship", "Steering");
            Register("CalculateArrive", typeof(CalculateArriveParameters), CreateCalculateArriveAction, "Ship", "Steering");
            Register("CalculateFlee", typeof(CalculateFleeParameters), CreateCalculateFleeAction, "Ship", "Steering");
            Register("ApplySteering", typeof(ApplySteeringParameters), CreateApplySteeringAction, "Ship", "Steering");
            Register("CalculateSmartArrive", typeof(CalculateSmartArriveParameters), CreateCalculateSmartArriveAction, "Ship", "Steering");
            Register("CalculateRecovery", typeof(CalculateRecoveryParameters), CreateCalculateRecoveryAction, "Ship", "Steering");
            Register("CalculateFlyThrough", typeof(CalculateFlyThroughParameters), CreateCalculateFlyThroughAction, "Ship", "Steering");
            Register("CalculateBrakeAndTurn", typeof(CalculateBrakeAndTurnParameters), CreateCalculateBrakeAndTurnAction, "Ship", "Steering");
            Register("CalculateMaintainDistance", typeof(CalculateMaintainDistanceParameters), CreateCalculateMaintainDistanceAction, "Ship", "Steering");
            Register("SetHoldingPosition", typeof(SetHoldingPositionParameters), CreateSetHoldingPositionAction, "Ship", "Steering");
            Register("SetTargetPositionFromEntity", typeof(SetTargetPositionFromEntityParameters), CreateSetTargetPositionFromEntityAction, "Ship", "Steering");

            // Ship > Rotation
            Register("RotateTowardTarget", typeof(RotateTowardTargetParameters), CreateRotateTowardTargetAction, "Ship", "Rotation");
            Register("RotateTowardVelocity", typeof(RotateTowardVelocityParameters), CreateRotateTowardVelocityAction, "Ship", "Rotation");

            // Ship > Waypoints
            Register("AdvanceWaypointIndex", typeof(AdvanceWaypointIndexParameters), CreateAdvanceWaypointIndexAction, "Ship", "Waypoints");
            Register("InitWaypointStack", typeof(InitWaypointStackParameters), CreateInitWaypointStackAction, "Ship", "Waypoints");
            Register("GenerateRandomSubWaypoints", typeof(GenerateRandomSubWaypointsParameters), CreateGenerateRandomSubWaypointsAction, "Ship", "Waypoints");
            Register("PushWaypoints", typeof(PushWaypointsParameters), CreatePushWaypointsAction, "Ship", "Waypoints");
            Register("PopWaypoints", typeof(PopWaypointsParameters), CreatePopWaypointsAction, "Ship", "Waypoints");

            // Ship > Speed
            Register("SetCruiseSpeed", typeof(SetCruiseSpeedParameters), CreateSetCruiseSpeedAction, "Ship", "Speed");
            Register("SetDynamicCruiseSpeed", typeof(SetDynamicCruiseSpeedParameters), CreateSetDynamicCruiseSpeedAction, "Ship", "Speed");

            // Ship > Trajectory
            Register("CalculateTrajectoryPrediction", typeof(CalculateTrajectoryPredictionParameters), CreateCalculateTrajectoryPredictionAction, "Ship", "Trajectory");

            // Ship > Conditions > Waypoint
            Register("IsStackDepth", typeof(IsStackDepthParameters), CreateIsStackDepthCondition, "Ship", "Conditions/Waypoint");
            Register("IsSequenceComplete", typeof(IsSequenceCompleteParameters), CreateIsSequenceCompleteCondition, "Ship", "Conditions/Waypoint");

            // Ship > Conditions > Position
            Register("IsNearPosition", typeof(IsNearPositionParameters), CreateIsNearPositionCondition, "Ship", "Conditions/Position");
            Register("IsStopped", typeof(IsStoppedParameters), CreateIsStoppedCondition, "Ship", "Conditions/Position");

            // Ship > Conditions > Module
            Register("HasModuleType", typeof(HasModuleTypeParameters), CreateHasModuleTypeCondition, "Ship", "Conditions/Module");
            Register("HasModuleCategory", typeof(HasModuleCategoryParameters), CreateHasModuleCategoryCondition, "Ship", "Conditions/Module");
            Register("HasModuleSubCategory", typeof(HasModuleSubCategoryParameters), CreateHasModuleSubCategoryCondition, "Ship", "Conditions/Module");

            // Ship > Conditions > Target
            Register("HasTarget", typeof(HasTargetParameters), CreateHasTargetCondition, "Ship", "Conditions/Target");
            Register("IsInRange", typeof(IsInRangeParameters), CreateIsInRangeCondition, "Ship", "Conditions/Target");

            // Ship > Conditions > Trajectory
            Register("WillMissTarget", typeof(WillMissTargetParameters), CreateWillMissTargetCondition, "Ship", "Conditions/Trajectory");
            Register("HasOvershot", typeof(HasOvershotParameters), CreateHasOvershotCondition, "Ship", "Conditions/Trajectory");
            Register("IsHighApproachAngle", typeof(IsHighApproachAngleParameters), CreateIsHighApproachAngleCondition, "Ship", "Conditions/Trajectory");

            // Ship > Conditions > Random
            Register("RandomChance", typeof(RandomChanceParameters), CreateRandomChanceCondition, "Ship", "Conditions/Random");

            // Ship > Utility
            Register("Wait", typeof(WaitParameters), CreateWaitAction, "Ship", "Utility");

            // Ship > Sensors
            Register("ScanForContacts", typeof(ScanForContactsParameters), CreateScanForContactsAction, "Ship", "Sensors");
            Register("SelectClosestContact", typeof(SelectClosestContactParameters), CreateSelectClosestContactAction, "Ship", "Sensors");
            Register("UpdateTargetDetection", typeof(UpdateTargetDetectionParameters), CreateUpdateTargetDetectionAction, "Ship", "Sensors");
            Register("SetTargetFromInvestigation", typeof(SetTargetFromInvestigationParameters), CreateSetTargetFromInvestigationAction, "Ship", "Sensors");
            Register("AddToIdentifiedList", typeof(AddToIdentifiedListParameters), CreateAddToIdentifiedListAction, "Ship", "Sensors");
            Register("ClearInvestigationTarget", typeof(ClearInvestigationTargetParameters), CreateClearInvestigationTargetAction, "Ship", "Sensors");
            Register("SetTargetFromLastKnown", typeof(SetTargetFromLastKnownParameters), CreateSetTargetFromLastKnownAction, "Ship", "Sensors");
            Register("ClearLastKnownPosition", typeof(ClearLastKnownPositionParameters), CreateClearLastKnownPositionAction, "Ship", "Sensors");
            Register("StoreMonitoredTarget", typeof(StoreMonitoredTargetParameters), CreateStoreMonitoredTargetAction, "Ship", "Sensors");

            // Ship > Conditions > Sensors
            Register("HasInvestigationTarget", typeof(HasInvestigationTargetParameters), CreateHasInvestigationTargetCondition, "Ship", "Conditions/Sensors");
            Register("IsTargetDetectionLevel", typeof(IsTargetDetectionLevelParameters), CreateIsTargetDetectionLevelCondition, "Ship", "Conditions/Sensors");
            Register("IsTargetHostile", typeof(IsTargetHostileParameters), CreateIsTargetHostileCondition, "Ship", "Conditions/Sensors");
            Register("HasLastKnownPosition", typeof(HasLastKnownPositionParameters), CreateHasLastKnownPositionCondition, "Ship", "Conditions/Sensors");
        }

        /// <summary>
        /// Register a new action type with entity type and category.
        /// </summary>
        public static void Register(string actionType, Type parameterType, ActionFactory factory, string entityType = "Ship", string category = "General")
        {
            _actions[actionType] = new ActionInfo(actionType, parameterType, factory, entityType, category);
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

        /// <summary>
        /// Get all registered action infos.
        /// </summary>
        public static IEnumerable<ActionInfo> GetAllActionInfos()
        {
            return _actions.Values;
        }

        /// <summary>
        /// Get all unique entity types.
        /// </summary>
        public static IEnumerable<string> GetEntityTypes()
        {
            return _actions.Values.Select(a => a.EntityType).Distinct().OrderBy(e => e);
        }

        /// <summary>
        /// Get all actions for a specific entity type, grouped by category.
        /// </summary>
        public static IEnumerable<IGrouping<string, ActionInfo>> GetActionsByEntityType(string entityType)
        {
            return _actions.Values
                .Where(a => a.EntityType == entityType)
                .GroupBy(a => a.Category)
                .OrderBy(g => g.Key);
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
            return new WaitAction(p.duration, p.randomDelay, p.elapsedKey);
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

        // Factory methods for sensor actions

        private static IBTNode CreateScanForContactsAction(IBTNodeParameters parameters)
        {
            var p = parameters as ScanForContactsParameters ?? new ScanForContactsParameters();
            return new ScanForContactsAction(p.minDetectionLevel, p.maxResults, p.outputKey, p.excludeIdentifiedKey);
        }

        private static IBTNode CreateSelectClosestContactAction(IBTNodeParameters parameters)
        {
            var p = parameters as SelectClosestContactParameters ?? new SelectClosestContactParameters();
            return new SelectClosestContactAction(p.inputKey, p.targetKey);
        }

        private static IBTNode CreateUpdateTargetDetectionAction(IBTNodeParameters parameters)
        {
            var p = parameters as UpdateTargetDetectionParameters ?? new UpdateTargetDetectionParameters();
            return new UpdateTargetDetectionAction(p.targetKey, p.levelKey, p.lastPositionKey);
        }

        private static IBTNode CreateSetTargetFromInvestigationAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetTargetFromInvestigationParameters ?? new SetTargetFromInvestigationParameters();
            return new SetTargetFromInvestigationAction(p.targetKey, p.steeringTargetKey);
        }

        private static IBTNode CreateAddToIdentifiedListAction(IBTNodeParameters parameters)
        {
            var p = parameters as AddToIdentifiedListParameters ?? new AddToIdentifiedListParameters();
            return new AddToIdentifiedListAction(p.targetKey, p.listKey);
        }

        private static IBTNode CreateClearInvestigationTargetAction(IBTNodeParameters parameters)
        {
            var p = parameters as ClearInvestigationTargetParameters ?? new ClearInvestigationTargetParameters();
            return new ClearInvestigationTargetAction(p.targetKey);
        }

        private static IBTNode CreateSetTargetFromLastKnownAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetTargetFromLastKnownParameters ?? new SetTargetFromLastKnownParameters();
            return new SetTargetFromLastKnownAction(p.lastPositionKey, p.steeringTargetKey);
        }

        private static IBTNode CreateClearLastKnownPositionAction(IBTNodeParameters parameters)
        {
            var p = parameters as ClearLastKnownPositionParameters ?? new ClearLastKnownPositionParameters();
            return new ClearLastKnownPositionAction(p.lastPositionKey);
        }

        // Factory methods for sensor conditions

        private static IBTNode CreateHasInvestigationTargetCondition(IBTNodeParameters parameters)
        {
            var p = parameters as HasInvestigationTargetParameters ?? new HasInvestigationTargetParameters();
            var condition = new HasInvestigationTargetCondition(p.targetKey);
            condition.Invert = p.invert;
            return condition;
        }

        private static IBTNode CreateIsTargetDetectionLevelCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsTargetDetectionLevelParameters ?? new IsTargetDetectionLevelParameters();
            return new IsTargetDetectionLevelCondition(p.levelKey, p.minLevel);
        }

        private static IBTNode CreateIsTargetHostileCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsTargetHostileParameters ?? new IsTargetHostileParameters();
            return new IsTargetHostileCondition(p.targetKey);
        }

        private static IBTNode CreateHasLastKnownPositionCondition(IBTNodeParameters parameters)
        {
            var p = parameters as HasLastKnownPositionParameters ?? new HasLastKnownPositionParameters();
            return new HasLastKnownPositionCondition(p.lastPositionKey);
        }

        private static IBTNode CreateCalculateMaintainDistanceAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateMaintainDistanceParameters ?? new CalculateMaintainDistanceParameters();
            return new CalculateMaintainDistanceAction(
                p.targetKey,
                p.outputKey,
                p.bufferDistance,
                p.correctionFactor,
                p.lateralBrakingFactor);
        }

        private static IBTNode CreateStoreMonitoredTargetAction(IBTNodeParameters parameters)
        {
            var p = parameters as StoreMonitoredTargetParameters ?? new StoreMonitoredTargetParameters();
            return new StoreMonitoredTargetAction(p.sourceKey, p.targetKey);
        }

        private static IBTNode CreateSetHoldingPositionAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetHoldingPositionParameters ?? new SetHoldingPositionParameters();
            return new SetHoldingPositionAction(p.targetEntityKey, p.outputKey, p.bufferDistance);
        }

        private static IBTNode CreateSetTargetPositionFromEntityAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetTargetPositionFromEntityParameters ?? new SetTargetPositionFromEntityParameters();
            return new SetTargetPositionFromEntityAction(p.sourceKey, p.outputKey);
        }
    }
}
