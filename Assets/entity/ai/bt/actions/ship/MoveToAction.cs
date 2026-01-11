using Starfire.Entity.AI.Steering;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Moves to a target position read from the blackboard.
    /// Returns Success when arrived, Running while moving, Failure if no target.
    /// </summary>
    public class MoveToAction : BTAction
    {
        private readonly ArriveBehavior _arrive;
        private readonly float _arrivalThreshold;
        private readonly float _slowingMultiplier;
        private readonly string _targetKey;

        public MoveToAction(float arrivalThreshold, float slowingMultiplier = 5f, string targetBlackboardKey = "move_target")
        {
            _arrivalThreshold = arrivalThreshold;
            _slowingMultiplier = slowingMultiplier;
            _targetKey = targetBlackboardKey;

            _arrive = new ArriveBehavior
            {
                ArrivalRadius = arrivalThreshold,
                SlowingRadius = arrivalThreshold * slowingMultiplier
            };
        }

        public override BTNodeStatus Execute(float deltaTime)
        {
            // Try to get target from blackboard
            if (!Context.TryGet<Vector2>(_targetKey, out var target))
            {
                return BTNodeStatus.Failure;
            }

            _arrive.Target = target;
            var steeringCtx = SteeringContext.FromShip(Context.Controller);
            var output = _arrive.Calculate(steeringCtx);

            // Check if arrived
            if (output.DistanceToTarget <= _arrivalThreshold)
            {
                Context.Driver.MovementDirection = Vector2.zero;
                return BTNodeStatus.Success;
            }

            // Apply steering
            Context.Driver.MovementDirection = output.SteeringForce.normalized;
            Context.Driver.AimPosition = target;

            return BTNodeStatus.Running;
        }

        public override void Reset()
        {
            // Nothing to reset - target comes from blackboard each frame
        }
    }
}