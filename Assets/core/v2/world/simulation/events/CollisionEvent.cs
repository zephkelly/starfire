using System;
using Starfire.Core.V2.World.Chunk;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation.Events
{
    /// <summary>
    /// Records a collision between two entities in the background simulation.
    /// </summary>
    [Serializable]
    public class CollisionEvent : SimulationEvent
    {
        /// <summary>Entity ID of the first entity in the collision.</summary>
        public int EntityIdA;

        /// <summary>Entity ID of the second entity in the collision.</summary>
        public int EntityIdB;

        /// <summary>Type of entity A (Asteroid, Ship, Station, etc.).</summary>
        public EntityType EntityTypeA;

        /// <summary>Type of entity B.</summary>
        public EntityType EntityTypeB;

        /// <summary>Velocity of entity A before collision.</summary>
        public Vector2D PreVelocityA;

        /// <summary>Velocity of entity B before collision.</summary>
        public Vector2D PreVelocityB;

        /// <summary>Velocity of entity A after collision (if survived).</summary>
        public Vector2D PostVelocityA;

        /// <summary>Velocity of entity B after collision (if survived).</summary>
        public Vector2D PostVelocityB;

        /// <summary>Relative velocity magnitude - how "hard" the collision was.</summary>
        public float Intensity;

        /// <summary>Kinetic energy of the impact.</summary>
        public float ImpactEnergy;

        /// <summary>What happened as a result of this collision.</summary>
        public CollisionResult Result;

        /// <summary>
        /// Entity ID of whoever originally caused the chain of events (e.g., player who pushed the asteroid).
        /// Null if the collision was from natural causes.
        /// </summary>
        public int? InstigatorEntityId;

        public CollisionEvent()
        {
            Type = SimulationEventType.Collision;
        }

        /// <summary>
        /// Creates a collision event with all required data.
        /// </summary>
        public static CollisionEvent Create(
            SimulatedEntity entityA,
            SimulatedEntity entityB,
            Vector2D postVelA,
            Vector2D postVelB,
            float intensity,
            float impactEnergy,
            CollisionResult result,
            double timestamp,
            double chunkSize)
        {
            var evt = new CollisionEvent
            {
                Timestamp = timestamp,
                Position = (entityA.AbsolutePosition + entityB.AbsolutePosition) * 0.5,
                EntityIdA = entityA.EntityId,
                EntityIdB = entityB.EntityId,
                EntityTypeA = entityA.EntityType,
                EntityTypeB = entityB.EntityType,
                PreVelocityA = entityA.Velocity,
                PreVelocityB = entityB.Velocity,
                PostVelocityA = postVelA,
                PostVelocityB = postVelB,
                Intensity = intensity,
                ImpactEnergy = impactEnergy,
                Result = result,
                InstigatorEntityId = entityA.InstigatorEntityId ?? entityB.InstigatorEntityId
            };

            evt.Chunk = ChunkCoord.FromAbsolutePosition(evt.Position, chunkSize);
            evt.NarrativeSummary = GenerateNarrativeSummary(evt);

            return evt;
        }

        private static string GenerateNarrativeSummary(CollisionEvent evt)
        {
            string entityAName = GetEntityTypeName(evt.EntityTypeA);
            string entityBName = GetEntityTypeName(evt.EntityTypeB);

            return evt.Result switch
            {
                CollisionResult.Bounced => $"{entityAName} collided with {entityBName}",
                CollisionResult.EntityADestroyed => $"{entityBName} destroyed {entityAName}",
                CollisionResult.EntityBDestroyed => $"{entityAName} destroyed {entityBName}",
                CollisionResult.BothDestroyed => $"{entityAName} and {entityBName} destroyed each other",
                _ => $"Collision between {entityAName} and {entityBName}"
            };
        }

        private static string GetEntityTypeName(EntityType type)
        {
            return type switch
            {
                EntityType.Asteroid => "an asteroid",
                EntityType.Ship => "a ship",
                EntityType.Station => "a station",
                EntityType.Projectile => "a projectile",
                _ => "an object"
            };
        }
    }
}
