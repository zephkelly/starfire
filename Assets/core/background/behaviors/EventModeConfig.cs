using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Core.Background.Behaviors
{
    /// <summary>
    /// Configuration for temporary event modes (meteor showers, space storms, etc.)
    /// Can be created at runtime or as ScriptableObjects for predefined events.
    /// </summary>
    [CreateAssetMenu(fileName = "EventMode", menuName = "Starfire/Background/Event Mode")]
    public class EventModeConfig : ScriptableObject
    {
        [Header("Event Identity")]
        public string eventId = "event";
        public string displayName = "Event";

        [Header("Spawn Overrides")]
        [Tooltip("Override spawn interval (-1 = use layer default)")]
        [SerializeField] private float spawnIntervalOverride = -1f;

        [Tooltip("Override max active stars (-1 = use layer default)")]
        [SerializeField] private int maxStarsOverride = -1;

        [Tooltip("Force all stars in this direction (zero = use normal direction logic)")]
        [SerializeField] private Vector2 directionOverride = Vector2.zero;

        [Header("Behavior Overrides")]
        [Tooltip("Use these behaviors instead of layer defaults (empty = use layer defaults)")]
        [SerializeField] private List<ShootingStarBehaviorConfig> behaviorOverrides = new List<ShootingStarBehaviorConfig>();

        /// <summary>
        /// Override spawn interval. Null if using layer default.
        /// </summary>
        public float? SpawnIntervalOverride => spawnIntervalOverride >= 0 ? spawnIntervalOverride : null;

        /// <summary>
        /// Override max stars. Null if using layer default.
        /// </summary>
        public int? MaxStarsOverride => maxStarsOverride >= 0 ? maxStarsOverride : null;

        /// <summary>
        /// Override direction. Null if using normal direction logic.
        /// </summary>
        public Vector2? DirectionOverride => directionOverride.sqrMagnitude > 0.001f ? directionOverride.normalized : null;

        /// <summary>
        /// Override behavior list. Empty list means use layer defaults.
        /// </summary>
        public List<ShootingStarBehaviorConfig> BehaviorOverrides => behaviorOverrides;

        /// <summary>
        /// Whether this event has behavior overrides.
        /// </summary>
        public bool HasBehaviorOverrides => behaviorOverrides != null && behaviorOverrides.Count > 0;

        /// <summary>
        /// Create a runtime event config (not saved as asset).
        /// </summary>
        public static EventModeConfig CreateRuntime(
            string id,
            float spawnInterval = -1f,
            int maxStars = -1,
            Vector2? direction = null,
            List<ShootingStarBehaviorConfig> behaviors = null)
        {
            var config = CreateInstance<EventModeConfig>();
            config.eventId = id;
            config.displayName = id;
            config.spawnIntervalOverride = spawnInterval;
            config.maxStarsOverride = maxStars;
            config.directionOverride = direction ?? Vector2.zero;
            config.behaviorOverrides = behaviors ?? new List<ShootingStarBehaviorConfig>();
            return config;
        }

        /// <summary>
        /// Builder pattern for creating runtime configs with fluent API.
        /// </summary>
        public class Builder
        {
            private string _id = "event";
            private float _spawnInterval = -1f;
            private int _maxStars = -1;
            private Vector2 _direction = Vector2.zero;
            private List<ShootingStarBehaviorConfig> _behaviors = new List<ShootingStarBehaviorConfig>();

            public Builder WithId(string id) { _id = id; return this; }
            public Builder WithSpawnInterval(float interval) { _spawnInterval = interval; return this; }
            public Builder WithMaxStars(int max) { _maxStars = max; return this; }
            public Builder WithDirection(Vector2 dir) { _direction = dir; return this; }
            public Builder WithBehavior(ShootingStarBehaviorConfig behavior) { _behaviors.Add(behavior); return this; }
            public Builder WithBehaviors(List<ShootingStarBehaviorConfig> behaviors) { _behaviors = behaviors; return this; }

            public EventModeConfig Build()
            {
                return CreateRuntime(_id, _spawnInterval, _maxStars, _direction, _behaviors);
            }
        }
    }
}
