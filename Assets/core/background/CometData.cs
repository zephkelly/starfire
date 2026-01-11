using UnityEngine;

namespace Starfire.Core.Background
{
    /// <summary>
    /// Data for a single active comet.
    /// Managed by CometLayer, passed to shader for rendering.
    /// </summary>
    [System.Serializable]
    public struct CometData
    {
        /// <summary>
        /// Starting position in world space (outside camera view).
        /// </summary>
        public Vector2 startPosition;

        /// <summary>
        /// Camera position when this comet was spawned (for parallax calculation).
        /// </summary>
        public Vector2 spawnCameraPosition;

        /// <summary>
        /// Current position in world space (nucleus position).
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
        /// Time.time when this comet was spawned.
        /// </summary>
        public float spawnTime;

        /// <summary>
        /// Total lifetime in seconds (time to fully cross and exit view).
        /// </summary>
        public float lifetime;

        /// <summary>
        /// Per-comet brightness multiplier.
        /// </summary>
        public float brightness;

        /// <summary>
        /// Trail length in world units.
        /// </summary>
        public float trailLength;

        /// <summary>
        /// Size of the bright nucleus core.
        /// </summary>
        public float nucleusSize;

        /// <summary>
        /// Size of the fuzzy coma glow surrounding the nucleus.
        /// </summary>
        public float comaSize;

        /// <summary>
        /// Seed for deterministic particle generation in the shader.
        /// </summary>
        public int particleSeed;

        /// <summary>
        /// Random phase offset for core pulsing animation.
        /// </summary>
        public float pulsePhase;

        /// <summary>
        /// Per-comet dust tail curve amount (can vary per comet).
        /// </summary>
        public float dustCurveAmount;

        /// <summary>
        /// Per-comet ion tail length multiplier.
        /// </summary>
        public float ionLengthMult;

        /// <summary>
        /// Progress through lifetime (0 = just spawned, 1 = should be removed).
        /// </summary>
        public float Progress => (Time.time - spawnTime) / lifetime;

        /// <summary>
        /// Whether this comet has completed its journey.
        /// </summary>
        public bool IsComplete => Progress >= 1f;

        /// <summary>
        /// Update the comet's position based on elapsed time.
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
        /// Create a new comet with calculated parameters.
        /// </summary>
        public static CometData Create(
            Vector2 startPos,
            Vector2 direction,
            float speed,
            float lifetime,
            float brightness,
            float trailLength,
            float nucleusSize,
            float comaSize,
            int particleSeed,
            Vector2 cameraPosition,
            float pulsePhase = -1f,
            float dustCurveAmount = -1f,
            float ionLengthMult = -1f)
        {
            // Generate random values for optional parameters
            if (pulsePhase < 0f) pulsePhase = Random.Range(0f, Mathf.PI * 2f);
            if (dustCurveAmount < 0f) dustCurveAmount = Random.Range(0.7f, 1.3f);
            if (ionLengthMult < 0f) ionLengthMult = Random.Range(0.8f, 1.2f);

            return new CometData
            {
                startPosition = startPos,
                position = startPos,
                spawnCameraPosition = cameraPosition,
                direction = direction.normalized,
                speed = speed,
                spawnTime = Time.time,
                lifetime = lifetime,
                brightness = brightness,
                trailLength = trailLength,
                nucleusSize = nucleusSize,
                comaSize = comaSize,
                particleSeed = particleSeed,
                pulsePhase = pulsePhase,
                dustCurveAmount = dustCurveAmount,
                ionLengthMult = ionLengthMult
            };
        }
    }
}
