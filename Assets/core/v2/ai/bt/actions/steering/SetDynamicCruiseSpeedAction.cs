using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Dynamically calculates cruise speed based on distance to current waypoint
    /// and the length of the next waypoint segment.
    /// Speed is expressed as a percentage of ship max speed, interpolated between
    /// min/max bounds based on effective distance.
    /// Reads speed/acceleration from blackboard heuristics.
    /// </summary>
    public class SetDynamicCruiseSpeedAction : BTAction
    {
        private readonly float _minSpeedPercent;
        private readonly float _maxSpeedPercent;
        private readonly float _minDistance;
        private readonly float _maxDistance;
        private readonly float _segmentInfluence;
        private readonly bool _scaleAcceleration;
        private readonly float _minAccelPercent;
        private readonly float _maxAccelPercent;
        private readonly string _targetKey;
        private readonly string _stackKey;
        private readonly string _speedKey;
        private readonly string _accelKey;

        public SetDynamicCruiseSpeedAction(
            float minSpeedPercent = 0.3f,
            float maxSpeedPercent = 0.8f,
            float minDistance = 10f,
            float maxDistance = 100f,
            float segmentInfluence = 0.5f,
            bool scaleAcceleration = false,
            float minAccelPercent = 0.5f,
            float maxAccelPercent = 1.0f,
            string targetKey = "steering_target",
            string stackKey = "waypoint_stack",
            string speedKey = "cruise_speed",
            string accelKey = "cruise_acceleration")
        {
            _minSpeedPercent = Mathf.Clamp01(minSpeedPercent);
            _maxSpeedPercent = Mathf.Clamp01(maxSpeedPercent);
            _minDistance = Mathf.Max(0.1f, minDistance);
            _maxDistance = Mathf.Max(_minDistance + 0.1f, maxDistance);
            _segmentInfluence = Mathf.Clamp01(segmentInfluence);
            _scaleAcceleration = scaleAcceleration;
            _minAccelPercent = Mathf.Clamp01(minAccelPercent);
            _maxAccelPercent = Mathf.Clamp01(maxAccelPercent);
            _targetKey = targetKey;
            _stackKey = stackKey;
            _speedKey = speedKey;
            _accelKey = accelKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Check propulsion capability via perception layer
            if (!Context.TryGet<bool>(HeuristicKeys.HasPropulsionModule, out var hasPropulsion) || !hasPropulsion)
            {
                Context.Set(_speedKey, -1f);
                Context.Set(_accelKey, -1f);
                return BTNodeStatus.Success;
            }

            // Get propulsion values from heuristics
            Context.TryGet<float>(HeuristicKeys.MaxSpeed, out var shipMaxSpeed);
            Context.TryGet<float>(HeuristicKeys.MaxAcceleration, out var shipMaxAccel);

            float effectiveDistance = CalculateEffectiveDistance();

            float t = Mathf.InverseLerp(_minDistance, _maxDistance, effectiveDistance);
            float speedPercent = Mathf.Lerp(_minSpeedPercent, _maxSpeedPercent, t);
            float cruiseSpeed = speedPercent * shipMaxSpeed;

            Context.Set(_speedKey, cruiseSpeed);

            if (_scaleAcceleration)
            {
                float accelPercent = Mathf.Lerp(_minAccelPercent, _maxAccelPercent, t);
                float cruiseAccel = accelPercent * shipMaxAccel;
                Context.Set(_accelKey, cruiseAccel);
            }
            else
            {
                Context.Set(_accelKey, -1f);
            }

            return BTNodeStatus.Success;
        }

        private float CalculateEffectiveDistance()
        {
            Vector2 entityPos = (Vector2)Context.Transform.position;

            if (!Context.TryGet<Vector2>(_targetKey, out var currentTarget))
            {
                return _maxDistance;
            }

            float distanceToTarget = Vector2.Distance(entityPos, currentTarget);

            float segmentLength = GetNextSegmentLength(currentTarget);

            return distanceToTarget + (segmentLength * _segmentInfluence);
        }

        private float GetNextSegmentLength(Vector2 currentTarget)
        {
            if (!Context.TryGet<WaypointStackState>(_stackKey, out var stack) || stack.IsEmpty)
            {
                return 0f;
            }

            var sequence = stack.Current;
            if (sequence == null || sequence.Count < 2)
            {
                return 0f;
            }

            int currentIndex = sequence.CurrentIndex;
            int nextIndex = GetNextIndex(sequence, currentIndex);

            if (nextIndex == currentIndex)
            {
                return 0f;
            }

            Vector2? nextTarget = GetWaypointAt(sequence, nextIndex);
            if (!nextTarget.HasValue)
            {
                return 0f;
            }

            return Vector2.Distance(currentTarget, nextTarget.Value);
        }

        private int GetNextIndex(WaypointSequence sequence, int currentIndex)
        {
            int count = sequence.Count;
            if (count == 0) return currentIndex;

            switch (sequence.Mode)
            {
                case WaypointTraversalMode.Loop:
                    return (currentIndex + 1) % count;

                case WaypointTraversalMode.PingPong:
                    int direction = sequence.Direction;
                    int nextIdx = currentIndex + direction;
                    if (nextIdx >= count - 1 || nextIdx <= 0)
                    {
                        return Mathf.Clamp(nextIdx, 0, count - 1);
                    }
                    return nextIdx;

                case WaypointTraversalMode.Once:
                    if (currentIndex >= count - 1)
                    {
                        return currentIndex;
                    }
                    return currentIndex + 1;

                default:
                    return currentIndex;
            }
        }

        private Vector2? GetWaypointAt(WaypointSequence sequence, int index)
        {
            if (index < 0 || index >= sequence.Count)
            {
                return null;
            }

            if (sequence.IsDynamic && sequence.Transforms != null)
            {
                var t = sequence.Transforms[index];
                return t != null ? (Vector2)t.position : null;
            }

            if (sequence.Waypoints != null && index < sequence.Waypoints.Count)
            {
                return sequence.Waypoints[index];
            }

            return null;
        }
    }
}
