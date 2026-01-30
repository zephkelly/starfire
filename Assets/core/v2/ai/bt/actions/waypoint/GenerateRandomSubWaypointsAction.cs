using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Generates random sub-waypoints around a center point for exploration.
    /// Uses random scatter distribution (random angle and distance within radius bounds).
    /// </summary>
    public class GenerateRandomSubWaypointsAction : BTAction
    {
        private readonly string _centerKey;
        private readonly string _outputKey;
        private readonly int _count;
        private readonly float _minRadius;
        private readonly float _maxRadius;
        private readonly bool _avoidCenter;

        public GenerateRandomSubWaypointsAction(
            string centerKey = "steering_target",
            string outputKey = "generated_subwaypoints",
            int count = 3,
            float minRadius = 5f,
            float maxRadius = 15f,
            bool avoidCenter = true)
        {
            _centerKey = centerKey;
            _outputKey = outputKey;
            _count = Mathf.Max(1, count);
            _minRadius = Mathf.Max(0f, minRadius);
            _maxRadius = Mathf.Max(_minRadius, maxRadius);
            _avoidCenter = avoidCenter;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            // Get center position
            Vector2 center;
            if (!string.IsNullOrEmpty(_centerKey) && Context.TryGet<Vector2>(_centerKey, out var targetCenter))
            {
                center = targetCenter;
            }
            else
            {
                // Use ship's current position as center
                center = (Vector2)Context.Transform.position;
            }

            // Generate random waypoints
            var waypoints = new List<Vector2>();

            for (int i = 0; i < _count; i++)
            {
                Vector2 point = GenerateRandomPoint(center);
                waypoints.Add(point);
            }

            // Store generated waypoints
            Context.Set(_outputKey, waypoints);

            return BTNodeStatus.Success;
        }

        private Vector2 GenerateRandomPoint(Vector2 center)
        {
            // Random angle (0 to 360 degrees)
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // Random distance within radius bounds
            float distance = Random.Range(_minRadius, _maxRadius);

            // Calculate offset
            Vector2 offset = new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );

            return center + offset;
        }
    }
}
