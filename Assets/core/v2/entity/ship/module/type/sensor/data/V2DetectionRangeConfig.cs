using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Configuration for sensor detection ranges at different detail levels.
    /// </summary>
    [Serializable]
    public class V2DetectionRangeConfig
    {
        [Tooltip("Maximum detection range for basic presence detection.")]
        [SerializeField] private float maxRange = 100f;

        [Tooltip("Percentage of max range at which Silhouette detection is available (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float silhouetteRangePercent = 0.5f;

        [Tooltip("Percentage of max range at which Full detection is available (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float fullRangePercent = 0.2f;

        [Header("Passive Detection (Without Transponder)")]
        [Tooltip("Percentage of max range for passive Silhouette detection (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float passiveSilhouettePercent = 0.3f;

        [Tooltip("Percentage of max range for passive Full detection (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float passiveFullPercent = 0.1f;

        // Public accessors
        public float MaxRange => maxRange;
        public float SilhouetteRange => maxRange * silhouetteRangePercent;
        public float FullRange => maxRange * fullRangePercent;
        public float PassiveSilhouetteRange => maxRange * passiveSilhouettePercent;
        public float PassiveFullRange => maxRange * passiveFullPercent;

        /// <summary>
        /// Gets the detection level for a target at the given distance when transponder is active.
        /// </summary>
        public V2DetectionLevel GetLevelForDistance(float distance)
        {
            return GetLevelForDistance(distance, 1f);
        }

        /// <summary>
        /// Gets the detection level for a target at the given distance when transponder is active.
        /// </summary>
        /// <param name="distance">Distance to target.</param>
        /// <param name="tierMultiplier">Multiplier from sensor module tier.</param>
        public V2DetectionLevel GetLevelForDistance(float distance, float tierMultiplier)
        {
            float effectiveMaxRange = maxRange * tierMultiplier;

            if (distance > effectiveMaxRange)
                return V2DetectionLevel.None;

            float fullRange = effectiveMaxRange * fullRangePercent;
            if (distance <= fullRange)
                return V2DetectionLevel.Full;

            float silhouetteRange = effectiveMaxRange * silhouetteRangePercent;
            if (distance <= silhouetteRange)
                return V2DetectionLevel.Silhouette;

            return V2DetectionLevel.Presence;
        }

        /// <summary>
        /// Gets the detection level for a target without transponder (passive detection only).
        /// </summary>
        public V2DetectionLevel GetPassiveLevelForDistance(float distance, float tierMultiplier)
        {
            float effectiveMaxRange = maxRange * tierMultiplier;

            if (distance > effectiveMaxRange)
                return V2DetectionLevel.None;

            float passiveFullRange = effectiveMaxRange * passiveFullPercent;
            if (distance <= passiveFullRange)
                return V2DetectionLevel.Full;

            float passiveSilhouetteRange = effectiveMaxRange * passiveSilhouettePercent;
            if (distance <= passiveSilhouetteRange)
                return V2DetectionLevel.Silhouette;

            return V2DetectionLevel.Presence;
        }

        /// <summary>
        /// Creates a default range configuration.
        /// </summary>
        public static V2DetectionRangeConfig CreateDefault()
        {
            return new V2DetectionRangeConfig
            {
                maxRange = 100f,
                silhouetteRangePercent = 0.5f,
                fullRangePercent = 0.2f,
                passiveSilhouettePercent = 0.3f,
                passiveFullPercent = 0.1f
            };
        }
    }
}
