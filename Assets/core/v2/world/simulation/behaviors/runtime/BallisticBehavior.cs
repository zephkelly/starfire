using System;
using Starfire.Core.V2.World;
using Starfire.Core.V2.World.Simulation.Behaviors.Configs;
using UnityEngine;

namespace Starfire.Core.V2.World.Simulation.Behaviors.Runtime
{
    /// <summary>
    /// Runtime implementation of ballistic (inertia + drag) behavior.
    /// Entities coast along their current velocity with optional drag applied.
    /// Supports both Tier 1 (per-frame) and Tier 2 (analytical prediction).
    /// </summary>
    public class BallisticBehavior : ISimulationBehavior
    {
        private readonly BallisticBehaviorConfig _config;

        public SimulatedBehaviorType Type => SimulatedBehaviorType.Ballistic;
        public SimulationBehaviorConfig Config => _config;

        public BallisticBehavior(BallisticBehaviorConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Update entity state for one simulation frame (Tier 1).
        /// Applies drag to velocity and integrates position using Euler method.
        /// </summary>
        public void UpdateEntity(SimulatedEntity entity, float deltaTime, ISimulationContext context)
        {
            // Get drag value
            float drag = _config.UseEntityDrag ? entity.Drag : _config.DefaultDrag;
            float mass = entity.Mass > 0f ? entity.Mass : 1f;

            // Apply linear drag: v' = v * (1 - drag/mass * dt)
            if (drag > 0f)
            {
                double dragFactor = 1.0 - (drag / mass) * deltaTime;
                dragFactor = Math.Max(0.0, dragFactor);
                entity.Velocity = entity.Velocity * dragFactor;
            }

            // Check velocity threshold
            if (_config.StopAtThreshold && entity.Velocity.SqrMagnitude < _config.MinVelocityThreshold * _config.MinVelocityThreshold)
            {
                entity.Velocity = Vector2D.Zero;
            }

            // Euler integration for position
            entity.AbsolutePosition += entity.Velocity * deltaTime;

            // Handle rotation
            if (_config.ApplyAngularVelocity && entity.AngularVelocity != 0f)
            {
                // Apply angular drag
                if (_config.AngularDrag > 0f)
                {
                    float angularDragFactor = 1f - (_config.AngularDrag / mass) * deltaTime;
                    angularDragFactor = Mathf.Max(0f, angularDragFactor);
                    entity.AngularVelocity *= angularDragFactor;
                }

                // Update rotation
                entity.Rotation += entity.AngularVelocity * deltaTime;

                // Normalize rotation to [0, 360)
                while (entity.Rotation < 0f) entity.Rotation += 360f;
                while (entity.Rotation >= 360f) entity.Rotation -= 360f;
            }
        }

        /// <summary>
        /// Predict entity position at a future time (Tier 2).
        /// Uses analytical solution for drag-affected motion.
        /// </summary>
        public Vector2D PredictPosition(SimulatedEntity entity, BehaviorSnapshot snapshot, double elapsedTime)
        {
            if (elapsedTime <= 0) return entity.AbsolutePosition;

            float drag = _config.UseEntityDrag ? entity.Drag : _config.DefaultDrag;
            float mass = entity.Mass > 0f ? entity.Mass : 1f;

            // If no drag, simple linear extrapolation
            if (drag < 0.0001f || mass < 0.0001f)
            {
                return entity.AbsolutePosition + entity.Velocity * elapsedTime;
            }

            // With drag: p(t) = p0 + (v0 * m / d) * (1 - e^(-d*t/m))
            // This is the analytical solution for exponential drag
            double dragOverMass = drag / mass;
            double factor = (1.0 - Math.Exp(-dragOverMass * elapsedTime)) / dragOverMass;

            return entity.AbsolutePosition + entity.Velocity * factor;
        }

        /// <summary>
        /// Predict entity velocity at a future time (Tier 2).
        /// Uses analytical solution for exponential drag decay.
        /// </summary>
        public Vector2D PredictVelocity(SimulatedEntity entity, BehaviorSnapshot snapshot, double elapsedTime)
        {
            if (elapsedTime <= 0) return entity.Velocity;

            float drag = _config.UseEntityDrag ? entity.Drag : _config.DefaultDrag;
            float mass = entity.Mass > 0f ? entity.Mass : 1f;

            // If no drag, velocity is constant
            if (drag < 0.0001f || mass < 0.0001f)
            {
                return entity.Velocity;
            }

            // With drag: v(t) = v0 * e^(-d*t/m)
            double dragOverMass = drag / mass;
            double factor = Math.Exp(-dragOverMass * elapsedTime);

            var predictedVelocity = entity.Velocity * factor;

            // Apply threshold
            if (_config.StopAtThreshold && predictedVelocity.SqrMagnitude < _config.MinVelocityThreshold * _config.MinVelocityThreshold)
            {
                return Vector2D.Zero;
            }

            return predictedVelocity;
        }

        /// <summary>
        /// Predict entity rotation at a future time (Tier 2).
        /// </summary>
        public float PredictRotation(SimulatedEntity entity, BehaviorSnapshot snapshot, double elapsedTime)
        {
            if (elapsedTime <= 0 || !_config.ApplyAngularVelocity)
                return entity.Rotation;

            float mass = entity.Mass > 0f ? entity.Mass : 1f;

            // If no angular drag, simple linear extrapolation
            if (_config.AngularDrag < 0.0001f || mass < 0.0001f)
            {
                float rotation = entity.Rotation + entity.AngularVelocity * (float)elapsedTime;

                // Normalize to [0, 360)
                while (rotation < 0f) rotation += 360f;
                while (rotation >= 360f) rotation -= 360f;

                return rotation;
            }

            // With angular drag: use similar exponential decay
            // ω(t) = ω0 * e^(-d*t/m)
            // θ(t) = θ0 + (ω0 * m / d) * (1 - e^(-d*t/m))
            double dragOverMass = _config.AngularDrag / mass;
            double factor = (1.0 - Math.Exp(-dragOverMass * elapsedTime)) / dragOverMass;

            float rotation2 = entity.Rotation + entity.AngularVelocity * (float)factor;

            // Normalize to [0, 360)
            while (rotation2 < 0f) rotation2 += 360f;
            while (rotation2 >= 360f) rotation2 -= 360f;

            return rotation2;
        }

        /// <summary>
        /// Capture the current behavior state as a snapshot.
        /// For ballistic behavior, the snapshot is minimal since all state is in the entity.
        /// </summary>
        public BehaviorSnapshot CaptureSnapshot(SimulatedEntity entity, ISimulationContext context)
        {
            return new BehaviorSnapshot
            {
                Type = SimulatedBehaviorType.Ballistic,
                // Ballistic doesn't need additional snapshot data
                // Velocity and position are already on the entity
            };
        }

        /// <summary>
        /// Restore behavior state from a snapshot.
        /// For ballistic behavior, this is a no-op since state is on the entity.
        /// </summary>
        public void RestoreFromSnapshot(SimulatedEntity entity, BehaviorSnapshot snapshot, ISimulationContext context)
        {
            // Nothing to restore for ballistic - state is in entity position/velocity
        }

        /// <summary>
        /// Get prediction confidence at a given elapsed time.
        /// Ballistic predictions are highly confident since they're deterministic.
        /// </summary>
        public float GetPredictionConfidence(double elapsedTime)
        {
            if (elapsedTime <= 0) return 1f;

            // Ballistic predictions are very reliable
            // Only degrade confidence very slowly
            float maxTime = _config.MaxPredictionTime > 0 ? _config.MaxPredictionTime : 60f;
            float normalizedTime = (float)(elapsedTime / maxTime);

            return Mathf.Clamp01(1f - normalizedTime * (1f - _config.PredictionConfidence));
        }
    }
}
