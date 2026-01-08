using UnityEngine;
using Starfire.Core.Background.Behaviors;

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
        /// Behavior-specific runtime data.
        /// </summary>
        public ShootingStarBehaviorData behavior;

        /// <summary>
        /// Time when star started exit fade (for Persistent mode).
        /// Zero means not fading yet.
        /// </summary>
        public float exitFadeStartTime;

        /// <summary>
        /// Total distance traveled (for distance-based fade).
        /// </summary>
        public float distanceTraveled;

        /// <summary>
        /// Progress through lifetime (0 = just spawned, 1 = should be removed).
        /// </summary>
        public float Progress => lifetime > 0 ? (Time.time - spawnTime) / lifetime : 1f;

        /// <summary>
        /// Whether this star has completed its journey (standard behavior).
        /// </summary>
        public bool IsComplete => Progress >= 1f;

        /// <summary>
        /// Update the star's position based on elapsed time.
        /// </summary>
        public void UpdatePosition()
        {
            Vector2 previousPos = position;
            float elapsed = Time.time - spawnTime;
            position = startPosition + direction * speed * elapsed;
            distanceTraveled += Vector2.Distance(previousPos, position);
        }

        /// <summary>
        /// Get the tail end position of the trail.
        /// </summary>
        public Vector2 TailPosition => position - direction * trailLength;

        /// <summary>
        /// Calculate effective opacity based on behavior type and current state.
        /// </summary>
        /// <param name="isInView">Whether the star is currently in camera view.</param>
        /// <returns>Opacity value from 0 to 1.</returns>
        public float CalculateOpacity(bool isInView)
        {
            switch (behavior.behaviorType)
            {
                case ShootingStarBehaviorType.Standard:
                    return CalculateStandardOpacity();

                case ShootingStarBehaviorType.Persistent:
                    return CalculatePersistentOpacity(isInView);

                case ShootingStarBehaviorType.SlowFade:
                    return CalculateSlowFadeOpacity();

                default:
                    return 1f;
            }
        }

        private float CalculateStandardOpacity()
        {
            float progress = Progress;
            float fadeInEnd = behavior.fadeParam1;
            float fadeOutStart = behavior.fadeParam2;

            // Fade in during first portion
            float fadeIn = fadeInEnd > 0 ? Mathf.SmoothStep(0f, 1f, progress / fadeInEnd) : 1f;

            // Fade out during last portion
            float fadeOutRange = 1f - fadeOutStart;
            float fadeOut = fadeOutRange > 0 ? 1f - Mathf.SmoothStep(0f, 1f, (progress - fadeOutStart) / fadeOutRange) : 1f;

            return fadeIn * fadeOut;
        }

        private float CalculatePersistentOpacity(bool isInView)
        {
            // Always full brightness while in view and not fading
            if (isInView && !behavior.IsExitFading)
                return 1f;

            // If we haven't started exit fade yet
            if (exitFadeStartTime <= 0f)
                return 1f;

            // Calculate fade based on time since exit
            float timeSinceExit = Time.time - exitFadeStartTime;
            float gracePeriod = behavior.fadeParam2;
            float fadeDuration = behavior.fadeParam1;

            // Still in grace period
            if (timeSinceExit < gracePeriod)
                return 1f;

            // Fading
            float fadeTime = timeSinceExit - gracePeriod;
            return 1f - Mathf.Clamp01(fadeTime / fadeDuration);
        }

        private float CalculateSlowFadeOpacity()
        {
            bool isTimeBased = (int)behavior.fadeParam3 == 0;
            float fadeDelay = behavior.fadeParam2;
            float fadeAmount = behavior.fadeParam1;

            float fadeValue;
            if (isTimeBased)
            {
                float elapsed = Time.time - spawnTime;
                float delayedTime = elapsed - fadeDelay;
                if (delayedTime < 0f) return 1f;
                fadeValue = delayedTime / fadeAmount;
            }
            else
            {
                float delayedDistance = distanceTraveled - fadeDelay;
                if (delayedDistance < 0f) return 1f;
                fadeValue = delayedDistance / fadeAmount;
            }

            return 1f - Mathf.Clamp01(fadeValue);
        }

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
            float width,
            ShootingStarBehaviorData behavior)
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
                width = width,
                behavior = behavior,
                exitFadeStartTime = 0f,
                distanceTraveled = 0f
            };
        }

        /// <summary>
        /// Create a new shooting star with default (Standard) behavior.
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
            return Create(startPos, direction, speed, lifetime, brightness, trailLength, width,
                ShootingStarBehaviorData.CreateDefault());
        }
    }
}
