using System;
using System.Collections.Generic;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Chunk;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Tracks entities as ballistic snapshots. No per-frame cost.
    /// When queried, analytically computes where the entity would be now
    /// using drag-aware ballistic math.
    /// </summary>
    public class Tier2BallisticTracker
    {
        private readonly Dictionary<int, BallisticSnapshot> _snapshots = new();
        private readonly BackgroundSimulationConfig _config;

        public int SnapshotCount => _snapshots.Count;

        public Tier2BallisticTracker(BackgroundSimulationConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Create a snapshot from a SimulatedEntity and store it.
        /// </summary>
        public void AddEntity(SimulatedEntity entity, double currentTime)
        {
            var snapshot = new BallisticSnapshot
            {
                EntityId = entity.EntityId,
                EntityType = entity.EntityType,
                OriginChunk = entity.OriginChunk,
                SnapshotTime = currentTime,
                Position = entity.AbsolutePosition,
                Velocity = entity.Velocity,
                Rotation = entity.Rotation,
                AngularVelocity = entity.AngularVelocity,
                Mass = entity.Mass,
                Radius = entity.Radius,
                Drag = entity.Drag,
                Variant = entity.Variant,
                Seed = entity.Seed,
                SourceType = entity.SourceType
            };

            _snapshots[entity.EntityId] = snapshot;

            // Enforce max limit
            if (_snapshots.Count > _config.tier2MaxEntities)
            {
                TrimOldest();
            }
        }

        /// <summary>
        /// Predict an entity's current state from its snapshot.
        /// </summary>
        public SimulatedEntity PredictEntity(int entityId, double currentTime)
        {
            if (!_snapshots.TryGetValue(entityId, out var snapshot))
                return null;

            double elapsed = currentTime - snapshot.SnapshotTime;
            if (elapsed < 0) elapsed = 0;

            var predictedPos = PredictPosition(snapshot.Position, snapshot.Velocity, snapshot.Drag, snapshot.Mass, elapsed);
            var predictedVel = PredictVelocity(snapshot.Velocity, snapshot.Drag, snapshot.Mass, elapsed);
            float predictedRot = snapshot.Rotation + snapshot.AngularVelocity * (float)elapsed;

            return new SimulatedEntity
            {
                EntityId = snapshot.EntityId,
                EntityType = snapshot.EntityType,
                OriginChunk = snapshot.OriginChunk,
                CurrentChunk = ChunkCoord.FromAbsolutePosition(predictedPos, WorldGenerationService.Instance.ChunkSize),
                AbsolutePosition = predictedPos,
                Velocity = predictedVel,
                Rotation = predictedRot,
                AngularVelocity = snapshot.AngularVelocity,
                Mass = snapshot.Mass,
                Radius = snapshot.Radius,
                Drag = snapshot.Drag,
                CurrentTier = SimulationTier.Tier2Ballistic,
                LastSimulationTime = snapshot.SnapshotTime,
                HasBeenModified = true,
                Variant = snapshot.Variant,
                Seed = snapshot.Seed,
                SourceType = snapshot.SourceType
            };
        }

        /// <summary>
        /// Get the predicted chunk for an entity without creating a full SimulatedEntity.
        /// </summary>
        public ChunkCoord? PredictChunk(int entityId, double currentTime, double chunkSize)
        {
            if (!_snapshots.TryGetValue(entityId, out var snapshot))
                return null;

            double elapsed = currentTime - snapshot.SnapshotTime;
            if (elapsed < 0) elapsed = 0;

            var pos = PredictPosition(snapshot.Position, snapshot.Velocity, snapshot.Drag, snapshot.Mass, elapsed);
            return ChunkCoord.FromAbsolutePosition(pos, chunkSize);
        }

        public void RemoveEntity(int entityId)
        {
            _snapshots.Remove(entityId);
        }

        public bool HasEntity(int entityId) => _snapshots.ContainsKey(entityId);

        public IEnumerable<BallisticSnapshot> GetAllSnapshots() => _snapshots.Values;

        public void Clear() => _snapshots.Clear();

        // ── Ballistic math ──────────────────────────────────────────────

        /// <summary>
        /// Position with drag: p(t) = p0 + (v0 * m / d) * (1 - e^(-d*t/m))
        /// Without drag: p(t) = p0 + v0 * t
        /// </summary>
        private static Vector2D PredictPosition(Vector2D p0, Vector2D v0, float drag, float mass, double t)
        {
            if (drag < 0.0001f || mass < 0.0001f)
            {
                return p0 + v0 * t;
            }

            double factor = (1.0 - Math.Exp(-drag * t / mass)) * mass / drag;
            return p0 + v0 * factor;
        }

        /// <summary>
        /// Velocity with drag: v(t) = v0 * e^(-d*t/m)
        /// </summary>
        private static Vector2D PredictVelocity(Vector2D v0, float drag, float mass, double t)
        {
            if (drag < 0.0001f || mass < 0.0001f)
            {
                return v0;
            }

            double factor = Math.Exp(-drag * t / mass);
            return v0 * factor;
        }

        private void TrimOldest()
        {
            // Remove the oldest snapshot by SnapshotTime
            int oldestId = -1;
            double oldestTime = double.MaxValue;

            foreach (var kvp in _snapshots)
            {
                if (kvp.Value.SnapshotTime < oldestTime)
                {
                    oldestTime = kvp.Value.SnapshotTime;
                    oldestId = kvp.Key;
                }
            }

            if (oldestId >= 0)
                _snapshots.Remove(oldestId);
        }
    }
}
