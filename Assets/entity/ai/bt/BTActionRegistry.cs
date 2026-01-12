using System;
using System.Collections.Generic;

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
            // Register built-in actions

            // New waypoint actions (composable design)
            Register("SetWaypointTarget", typeof(SetWaypointTargetParameters), CreateSetWaypointTargetAction);
            Register("AdvanceWaypoint", typeof(AdvanceWaypointParameters), CreateAdvanceWaypointAction);

            // Legacy alias for backward compatibility - maps to idempotent SetWaypointTargetAction
            // Note: Existing behavior trees using SetNextWaypoint need to add AdvanceWaypoint node
            Register("SetNextWaypoint", typeof(SetNextWaypointParameters), CreateSetWaypointTargetFromLegacy);

            // Movement actions
            Register("MoveTo", typeof(MoveToParameters), CreateMoveToAction);
            Register("CalculateSteering", typeof(CalculateSteeringParameters), CreateCalculateSteeringAction);
            Register("ApplyMovement", typeof(ApplyMovementParameters), CreateApplyMovementAction);

            // Legacy alias - ApplySteering maps to ApplyMovement for backward compat
            Register("ApplySteering", typeof(ApplySteeringParameters), CreateApplyMovementFromLegacy);

            // Aim/Rotation actions (separate from movement)
            Register("AimAtTarget", typeof(AimAtTargetParameters), CreateAimAtTargetAction);
            Register("AimInMovementDirection", typeof(AimInMovementDirectionParameters), CreateAimInMovementDirectionAction);

            // Conditions
            Register("IsAtTarget", typeof(IsAtTargetParameters), CreateIsAtTargetCondition);
            Register("HasTarget", typeof(HasTargetParameters), CreateHasTargetCondition);
            Register("IsInRangeOfEntity", typeof(IsInRangeOfEntityParameters), CreateIsInRangeOfEntityCondition);

            // Entity targeting
            Register("SetEntityAsTarget", typeof(SetEntityAsTargetParameters), CreateSetEntityAsTargetAction);
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

        // Factory methods for built-in actions

        private static IBTNode CreateSetWaypointTargetAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetWaypointTargetParameters ?? new SetWaypointTargetParameters();
            return new SetWaypointTargetAction(p.waypointsKey, p.targetKey, p.indexKey);
        }

        private static IBTNode CreateAdvanceWaypointAction(IBTNodeParameters parameters)
        {
            var p = parameters as AdvanceWaypointParameters ?? new AdvanceWaypointParameters();
            return new AdvanceWaypointAction(p.waypointsKey, p.indexKey, p.traversalMode, p.directionKey);
        }

        /// <summary>
        /// Legacy factory - maps old SetNextWaypoint to new SetWaypointTarget.
        /// Existing behavior trees using SetNextWaypoint will get the idempotent version.
        /// They should be updated to use the new Sequence pattern for proper patrol behavior.
        /// </summary>
        private static IBTNode CreateSetWaypointTargetFromLegacy(IBTNodeParameters parameters)
        {
            var p = parameters as SetNextWaypointParameters ?? new SetNextWaypointParameters();
            return new SetWaypointTargetAction(p.waypointsKey, p.targetKey, p.indexKey);
        }

        private static IBTNode CreateMoveToAction(IBTNodeParameters parameters)
        {
            var p = parameters as MoveToParameters ?? new MoveToParameters();
            return new MoveToAction(p.arrivalThreshold, p.slowingMultiplier, p.targetKey);
        }

        private static IBTNode CreateCalculateSteeringAction(IBTNodeParameters parameters)
        {
            var p = parameters as CalculateSteeringParameters ?? new CalculateSteeringParameters();
            return new CalculateSteeringAction(p);
        }

        private static IBTNode CreateApplyMovementAction(IBTNodeParameters parameters)
        {
            var p = parameters as ApplyMovementParameters ?? new ApplyMovementParameters();
            return new ApplyMovementAction(p);
        }

        /// <summary>
        /// Legacy factory - maps old ApplySteering to new ApplyMovement.
        /// </summary>
        private static IBTNode CreateApplyMovementFromLegacy(IBTNodeParameters parameters)
        {
            // Convert old parameters to new format
            var legacy = parameters as ApplySteeringParameters;
            var p = new ApplyMovementParameters
            {
                directionKey = legacy?.directionKey ?? "steering_direction",
                throttleKey = legacy?.throttleKey ?? "steering_throttle"
            };
            return new ApplyMovementAction(p);
        }

        private static IBTNode CreateAimAtTargetAction(IBTNodeParameters parameters)
        {
            var p = parameters as AimAtTargetParameters ?? new AimAtTargetParameters();
            return new AimAtTargetAction(p.targetKey);
        }

        private static IBTNode CreateAimInMovementDirectionAction(IBTNodeParameters parameters)
        {
            var p = parameters as AimInMovementDirectionParameters ?? new AimInMovementDirectionParameters();
            return new AimInMovementDirectionAction(p.lookAheadDistance);
        }

        private static IBTNode CreateIsAtTargetCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsAtTargetParameters ?? new IsAtTargetParameters();
            return new IsAtTargetCondition(p.targetKey, p.threshold);
        }

        private static IBTNode CreateHasTargetCondition(IBTNodeParameters parameters)
        {
            var p = parameters as HasTargetParameters ?? new HasTargetParameters();
            return new HasTargetCondition(p.targetKey);
        }

        private static IBTNode CreateIsInRangeOfEntityCondition(IBTNodeParameters parameters)
        {
            var p = parameters as IsInRangeOfEntityParameters ?? new IsInRangeOfEntityParameters();
            return new IsInRangeOfEntityCondition(p.entityKey, p.minRange, p.maxRange);
        }

        private static IBTNode CreateSetEntityAsTargetAction(IBTNodeParameters parameters)
        {
            var p = parameters as SetEntityAsTargetParameters ?? new SetEntityAsTargetParameters();
            return new SetEntityAsTargetAction(p.entityKey, p.targetKey);
        }
    }
}
