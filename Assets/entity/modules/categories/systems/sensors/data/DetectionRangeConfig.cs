using System;
using UnityEngine;

namespace Starfire.Entity.Modules.Sensor
{
    [Serializable]
    public class DetectionRangeConfig
    {
        [Tooltip("Maximum detection range (Presence level)")]
        public float maxRange = 100f;

        [Tooltip("Range for Silhouette detection (faction/class visible)")]
        [Range(0f, 1f)]
        public float silhouetteRangePercent = 0.6f;

        [Tooltip("Range for Full detection (complete data)")]
        [Range(0f, 1f)]
        public float fullRangePercent = 0.3f;

        public float SilhouetteRange => maxRange * silhouetteRangePercent;
        public float FullRange => maxRange * fullRangePercent;

        public DetectionLevel GetLevelForDistance(float distance)
        {
            if (distance > maxRange)
                return DetectionLevel.None;

            if (distance <= FullRange)
                return DetectionLevel.Full;

            if (distance <= SilhouetteRange)
                return DetectionLevel.Silhouette;

            return DetectionLevel.Presence;
        }

        public DetectionLevel GetLevelForDistance(float distance, float tierMultiplier)
        {
            float effectiveMaxRange = maxRange * tierMultiplier;
            float effectiveSilhouetteRange = SilhouetteRange * tierMultiplier;
            float effectiveFullRange = FullRange * tierMultiplier;

            if (distance > effectiveMaxRange)
                return DetectionLevel.None;

            if (distance <= effectiveFullRange)
                return DetectionLevel.Full;

            if (distance <= effectiveSilhouetteRange)
                return DetectionLevel.Silhouette;

            return DetectionLevel.Presence;
        }
    }
}
