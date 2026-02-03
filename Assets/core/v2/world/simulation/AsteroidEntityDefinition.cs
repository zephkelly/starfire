using System;
using System.Text;
using UnityEngine;
using Starfire.Core.V2.World.Data;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Wrapper that adapts AsteroidDefinition to the ISimulatableDefinition interface.
    /// Allows asteroids to work with the generic entity tracking system.
    /// </summary>
    public class AsteroidEntityDefinition : ISimulatableDefinition
    {
        private readonly AsteroidDefinition _asteroid;

        public AsteroidEntityDefinition(AsteroidDefinition asteroid)
        {
            _asteroid = asteroid;
        }

        public EntityType EntityType => EntityType.Asteroid;
        public Vector2 LocalPosition => _asteroid.LocalPosition;
        public float Size => _asteroid.Size;
        public float Rotation => _asteroid.Rotation;

        // Asteroid-specific properties
        public int Variant => _asteroid.Variant;
        public float Seed => _asteroid.Seed;
        public AsteroidSource Source => _asteroid.Source;

        public bool Matches(ISimulatableDefinition other, float positionTolerance = 0.01f)
        {
            if (other == null || other.EntityType != EntityType.Asteroid)
                return false;

            // Position is the primary matching criteria
            float dx = Mathf.Abs(LocalPosition.x - other.LocalPosition.x);
            float dy = Mathf.Abs(LocalPosition.y - other.LocalPosition.y);

            if (dx > positionTolerance || dy > positionTolerance)
                return false;

            // For asteroids, also check size to differentiate overlapping asteroids
            if (other is AsteroidEntityDefinition asteroidDef)
            {
                return Mathf.Approximately(Size, asteroidDef.Size);
            }

            return Mathf.Approximately(Size, other.Size);
        }

        public long GetPositionHash(float quantizationScale = 100f)
        {
            int qx = Mathf.RoundToInt(LocalPosition.x * quantizationScale);
            int qy = Mathf.RoundToInt(LocalPosition.y * quantizationScale);
            return ((long)qx << 32) | (uint)qy;
        }

        public byte[] SerializeTypeData()
        {
            // Simple serialization for asteroid-specific data
            var data = $"{Variant}|{Seed}|{(int)Source}";
            return Encoding.UTF8.GetBytes(data);
        }

        public void DeserializeTypeData(byte[] data)
        {
            // Note: AsteroidDefinition is typically a struct so we can't modify _asteroid
            // This method is primarily for future extensibility
        }

        /// <summary>
        /// Convert back to the original AsteroidDefinition.
        /// </summary>
        public AsteroidDefinition ToAsteroidDefinition()
        {
            return _asteroid;
        }

        /// <summary>
        /// Create an AsteroidEntityDefinition from position and properties.
        /// </summary>
        public static AsteroidEntityDefinition Create(
            Vector2 localPosition,
            float size,
            float rotation,
            int variant,
            float seed,
            AsteroidSource source)
        {
            return new AsteroidEntityDefinition(new AsteroidDefinition
            {
                LocalPosition = localPosition,
                Size = size,
                Rotation = rotation,
                Variant = variant,
                Seed = seed,
                Source = source
            });
        }
    }
}
