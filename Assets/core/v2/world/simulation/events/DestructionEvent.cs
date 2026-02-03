using System;
using Starfire.Core.V2.World.Chunk;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation.Events
{
    /// <summary>
    /// Records when an entity is destroyed in the background simulation.
    /// </summary>
    [Serializable]
    public class DestructionEvent : SimulationEvent
    {
        /// <summary>Entity ID of the destroyed entity.</summary>
        public int DestroyedEntityId;

        /// <summary>Type of the destroyed entity.</summary>
        public EntityType DestroyedType;

        /// <summary>Entity ID of what destroyed it (if applicable).</summary>
        public int? DestroyerEntityId;

        /// <summary>Type of the entity that caused destruction (if applicable).</summary>
        public EntityType? DestroyerType;

        /// <summary>
        /// Who originally set the destruction chain in motion (e.g., player who pushed an asteroid).
        /// </summary>
        public int? InstigatorEntityId;

        /// <summary>Kinetic energy of the impact that caused destruction.</summary>
        public float ImpactEnergy;

        /// <summary>Velocity of the destroyed entity at time of destruction.</summary>
        public Vector2D DestroyedVelocity;

        /// <summary>Mass of the destroyed entity (for debris calculation).</summary>
        public float DestroyedMass;

        /// <summary>Radius of the destroyed entity (for debris calculation).</summary>
        public float DestroyedRadius;

        public DestructionEvent()
        {
            Type = SimulationEventType.Destruction;
        }

        /// <summary>
        /// Creates a destruction event from a collision.
        /// </summary>
        public static DestructionEvent Create(
            SimulatedEntity destroyed,
            SimulatedEntity destroyer,
            float impactEnergy,
            int? instigatorId,
            double timestamp,
            double chunkSize)
        {
            var evt = new DestructionEvent
            {
                Timestamp = timestamp,
                Position = destroyed.AbsolutePosition,
                DestroyedEntityId = destroyed.EntityId,
                DestroyedType = destroyed.EntityType,
                DestroyerEntityId = destroyer?.EntityId,
                DestroyerType = destroyer?.EntityType,
                InstigatorEntityId = instigatorId,
                ImpactEnergy = impactEnergy,
                DestroyedVelocity = destroyed.Velocity,
                DestroyedMass = destroyed.Mass,
                DestroyedRadius = destroyed.Radius
            };

            evt.Chunk = ChunkCoord.FromAbsolutePosition(evt.Position, chunkSize);
            evt.NarrativeSummary = GenerateNarrativeSummary(evt);

            return evt;
        }

        private static string GenerateNarrativeSummary(DestructionEvent evt)
        {
            string destroyedName = GetEntityTypeName(evt.DestroyedType);

            if (evt.DestroyerType.HasValue)
            {
                string destroyerName = GetEntityTypeName(evt.DestroyerType.Value);
                return $"{destroyerName} destroyed {destroyedName}";
            }

            return $"{destroyedName} was destroyed";
        }

        private static string GetEntityTypeName(EntityType type)
        {
            return type switch
            {
                EntityType.Asteroid => "An asteroid",
                EntityType.Ship => "A ship",
                EntityType.Station => "A station",
                EntityType.Projectile => "A projectile",
                _ => "An object"
            };
        }
    }
}
