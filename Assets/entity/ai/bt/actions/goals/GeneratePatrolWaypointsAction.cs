using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Generates patrol waypoints around a center position if none exist.
    /// Enables self-bootstrapping AI that doesn't need external waypoint setup.
    /// Returns Success if waypoints are available (existing or newly generated).
    /// </summary>
    public class GeneratePatrolWaypointsAction : BTAction
    {
        private readonly string _outputKey;
        private readonly string _centerKey;
        private readonly string _radiusKey;
        private readonly float _defaultRadius;
        private readonly int _waypointCount;
        private readonly bool _skipIfExists;

        public GeneratePatrolWaypointsAction(
            string outputKey = "waypoint_list",
            string centerKey = "patrol_center",
            string radiusKey = "patrol_radius",
            float defaultRadius = 50f,
            int waypointCount = 4,
            bool skipIfExists = true)
        {
            _outputKey = outputKey;
            _centerKey = centerKey;
            _radiusKey = radiusKey;
            _defaultRadius = defaultRadius;
            _waypointCount = waypointCount;
            _skipIfExists = skipIfExists;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Skip if waypoints already exist
            if (_skipIfExists && Context.Has(_outputKey))
            {
                return BTNodeStatus.Success;
            }

            // Get center (from blackboard or current position)
            Vector2 center = Context.TryGet<Vector2>(_centerKey, out var c)
                ? c
                : Context.Transform.position;

            // Get radius (from blackboard or default)
            float radius = Context.TryGet<float>(_radiusKey, out var r)
                ? r
                : _defaultRadius;

            // Generate evenly-spaced waypoints in a circle with some randomization
            var waypoints = new List<Vector2>();
            for (int i = 0; i < _waypointCount; i++)
            {
                float angle = (i / (float)_waypointCount) * Mathf.PI * 2f;
                // Add randomization to make patterns less predictable
                float randomRadius = radius * Random.Range(0.8f, 1.2f);
                float randomAngle = angle + Random.Range(-0.2f, 0.2f);

                waypoints.Add(center + new Vector2(
                    Mathf.Cos(randomAngle) * randomRadius,
                    Mathf.Sin(randomAngle) * randomRadius
                ));
            }

            Context.Set(_outputKey, waypoints);

            // Store center for reference if not already set
            if (!Context.Has(_centerKey))
            {
                Context.Set(_centerKey, center);
            }

            return BTNodeStatus.Success;
        }
    }
}
