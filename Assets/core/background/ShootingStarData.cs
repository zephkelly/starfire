using UnityEngine;

namespace Starfire.Core.Background
{
    /// <summary>
    /// Data for a single active shooting star.
    /// Managed by ShootingStarLayer, passed to shader for rendering.
    /// </summary>
    [System.Serializable]
    public struct ShootingStarData
    {
        /// <summary>
        /// Starting position in world space (outside camera view).
        /// </summary>
        public Vector2 startPosition;

        /// <summary>
        /// Current position in world space.
        /// </summary>
        public Vector2 position;

        /// <summary>
        /// Normalized direction of travel.
        /// </summary>
        public Vector2 direction;

        /// <summary>
        /// Speed in world units per second.
        /// </summary>
        public float speed;

        /// <summary>
        /// Time.time when this star was spawned.
        /// </summary>
        public float spawnTime;

        /// <summary>
        /// Total lifetime in seconds (time to fully cross and exit view).
        /// </summary>
        public float lifetime;

        /// <summary>
        /// Per-star brightness multiplier.
        /// </summary>
        public float brightness;

        /// <summary>
        /// Trail length in world units.
        /// </summary>
        public float trailLength;

        /// <summary>
        /// Width of this shooting star.
        /// </summary>
        public float width;

        /// <summary>
        /// Progress through lifetime (0 = just spawned, 1 = should be removed).
        /// </summary>
        public float Progress => (Time.time - spawnTime) / lifetime;

        /// <summary>
        /// Whether this star has completed its journey.
        /// </summary>
        public bool IsComplete => Progress >= 1f;

        /// <summary>
        /// Update the star's position based on elapsed time.
        /// </summary>
        public void UpdatePosition()
        {
            float elapsed = Time.time - spawnTime;
            position = startPosition + direction * speed * elapsed;
        }

        /// <summary>
        /// Get the tail end position of the trail.
        /// </summary>
        public Vector2 TailPosition => position - direction * trailLength;

        /// <summary>
        /// Create a new shooting star with calculated parameters.
        /// </summary>
        public static ShootingStarData Create(
            Vector2 startPos,
            Vector2 direction,
            float speed,
            float lifetime,
            float brightness,
            float trailLength,
            float width)
        {
            return new ShootingStarData
            {
                startPosition = startPos,
                position = startPos,
                direction = direction.normalized,
                speed = speed,
                spawnTime = Time.time,
                lifetime = lifetime,
                brightness = brightness,
                trailLength = trailLength,
                width = width
            };
        }
    }
}
