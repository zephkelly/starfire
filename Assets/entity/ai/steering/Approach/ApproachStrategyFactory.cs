using System;
using System.Collections.Generic;

namespace Starfire.Entity.AI.Steering
{
    /// <summary>
    /// Factory for creating approach strategy instances by type.
    /// Caches instances for reuse since strategies are stateless.
    /// </summary>
    public static class ApproachStrategyFactory
    {
        private static readonly Dictionary<ApproachStrategyType, IApproachStrategy> _cache = new();

        /// <summary>
        /// Creates or retrieves a cached approach strategy instance.
        /// </summary>
        public static IApproachStrategy Create(ApproachStrategyType type)
        {
            if (_cache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            IApproachStrategy strategy = type switch
            {
                ApproachStrategyType.Controlled => new ControlledApproachStrategy(),
                ApproachStrategyType.FastBrake => new FastBrakeApproachStrategy(),
                ApproachStrategyType.Flyby => new FlybyApproachStrategy(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown approach strategy type")
            };

            _cache[type] = strategy;
            return strategy;
        }

        /// <summary>
        /// Clears the strategy cache. Useful for testing or hot-reloading.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}
