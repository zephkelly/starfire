using System;
using UnityEngine;
using Starfire.Core.V2.Save;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Simulation;
using StarfireV2;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Abstract base class for runtime entity trackers.
    /// Provides common functionality for monitoring entity state changes and chunk boundary crossings.
    /// Derive from this class to create trackers for specific entity types (asteroid, ship, projectile, etc.).
    /// </summary>
    public abstract class RuntimeEntityTrackerBase : MonoBehaviour, IRuntimeEntityTracker
    {
        // ── Abstract Members (Must Be Implemented) ──────────────────────────

        /// <summary>Type of entity being tracked.</summary>
        public abstract EntityType EntityType { get; }

        /// <summary>The procedural definition of this entity for save/restore matching.</summary>
        public abstract ISimulatableDefinition Definition { get; }

        /// <summary>
        /// Convert this runtime entity to a SimulatedEntity for background simulation.
        /// Implementations should populate type-specific data using SimulatedEntityExtensions.
        /// </summary>
        public abstract SimulatedEntity ToSimulatedEntity();

        // ── Common State ────────────────────────────────────────────────────

        /// <summary>Unique identifier for this entity.</summary>
        public int EntityId { get; protected set; }

        /// <summary>Chunk where this entity was originally spawned (procedural origin).</summary>
        public ChunkCoord OriginChunk { get; protected set; }

        /// <summary>Chunk where this entity currently resides.</summary>
        public ChunkCoord CurrentChunk { get; protected set; }

        /// <summary>Whether this entity has been modified from its procedural baseline.</summary>
        public bool HasBeenModified { get; protected set; }

        // ── Events ──────────────────────────────────────────────────────────

        /// <summary>
        /// Raised when the entity needs to be destroyed/pooled
        /// (e.g., when it escapes into an unloaded chunk).
        /// </summary>
        public event Action<IRuntimeEntityTracker> OnRequestDestroy;

        /// <summary>
        /// Raised when the entity crosses from one loaded chunk to another.
        /// </summary>
        public event Action<IRuntimeEntityTracker, ChunkCoord, ChunkCoord> OnChunkMigration;

        /// <summary>
        /// Raised when the entity's state is modified (e.g., after a collision).
        /// </summary>
        public event Action<IRuntimeEntityTracker> OnModified;

        // ── Protected State ─────────────────────────────────────────────────

        protected Rigidbody2D _rb;
        protected InstigatorTracker _instigatorTracker;
        protected bool _initialized;

        [SerializeField]
        protected float minModificationVelocity = 0.5f;

        // ── Unity Lifecycle ─────────────────────────────────────────────────

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _instigatorTracker = GetComponent<InstigatorTracker>();
        }

        protected virtual void FixedUpdate()
        {
            if (!_initialized || _rb == null) return;
            CheckChunkBoundary();
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_initialized) return;

            // Mark as modified if this is a significant collision
            if (collision.relativeVelocity.magnitude >= minModificationVelocity)
            {
                MarkAsModified();
            }
        }

        // ── Chunk Boundary Tracking ─────────────────────────────────────────

        /// <summary>
        /// Check if the entity has crossed into a different chunk.
        /// </summary>
        protected void CheckChunkBoundary()
        {
            var service = WorldGenerationService.Instance;
            var chunkManager = service?.ChunkManager;
            if (chunkManager == null) return;

            // Calculate current absolute position and chunk
            var absolutePos = service.WorldToAbsolute(transform.position);
            var newChunk = ChunkCoord.FromAbsolutePosition(absolutePos, service.ChunkSize);

            // Check if we've crossed into a different chunk
            if (newChunk != CurrentChunk)
            {
                HandleChunkBoundaryCrossing(newChunk, absolutePos, chunkManager);
            }
        }

        /// <summary>
        /// Handle crossing from one chunk to another.
        /// </summary>
        protected virtual void HandleChunkBoundaryCrossing(ChunkCoord newChunk, Vector2D absolutePos, ChunkManager chunkManager)
        {
            // Check if the new chunk is loaded
            if (!chunkManager.IsChunkLoaded(newChunk))
            {
                // Entering unloaded territory - transfer to background simulation
                TransferToSimulation(absolutePos);
                return;
            }

            // Both chunks are loaded - track as migration
            var oldChunk = CurrentChunk;
            NotifyChunkMigration(oldChunk, newChunk);
            CurrentChunk = newChunk;

            // Migration also counts as a modification
            MarkAsModified();
        }

        /// <summary>
        /// Transfer this entity to background simulation when leaving loaded chunks.
        /// </summary>
        protected virtual void TransferToSimulation(Vector2D absolutePos)
        {
            var simManager = BackgroundSimulationManager.Instance;
            var chunkTracker = SaveSystem.Instance?.ChunkTracker;

            if (simManager == null)
            {
                Debug.LogWarning($"[{GetType().Name}] Cannot transfer to simulation - BackgroundSimulationManager not available");
                return;
            }

            // Create the simulated entity
            var simEntity = ToSimulatedEntity();

            Debug.Log($"[{GetType().Name}] Transferring {EntityType} to simulation: ID={simEntity.EntityId}, " +
                      $"Pos=({absolutePos.X:F1}, {absolutePos.Y:F1}), " +
                      $"Vel=({_rb.linearVelocity.x:F2}, {_rb.linearVelocity.y:F2})");

            // Register with background simulation
            simManager.RegisterEntity(simEntity);

            // Mark origin chunk as having this entity removed
            if (chunkTracker != null && Definition != null)
            {
                chunkTracker.OnEntityDestroyed(OriginChunk, Definition);
            }

            // Request destruction/pooling of this GameObject
            OnRequestDestroy?.Invoke(this);
        }

        /// <summary>
        /// Notify the chunk tracker about migration between loaded chunks.
        /// </summary>
        protected virtual void NotifyChunkMigration(ChunkCoord fromChunk, ChunkCoord toChunk)
        {
            var chunkTracker = SaveSystem.Instance?.ChunkTracker;

            // Invoke migration event for consumers to handle
            OnChunkMigration?.Invoke(this, fromChunk, toChunk);

            if (chunkTracker != null && Definition != null)
            {
                // Mark as removed from origin chunk
                chunkTracker.OnEntityDestroyed(OriginChunk, Definition);

                // Mark as added to destination chunk with current velocity
                var velocity = _rb != null ? _rb.linearVelocity : Vector2.zero;
                var angularVelocity = _rb != null ? _rb.angularVelocity : 0f;
                chunkTracker.OnEntityAdded(toChunk, Definition, velocity, angularVelocity);
            }

            Debug.Log($"[{GetType().Name}] {EntityType} migrated from chunk ({fromChunk.X}, {fromChunk.Y}) to ({toChunk.X}, {toChunk.Y})");
        }

        // ── Modification Tracking ───────────────────────────────────────────

        /// <summary>
        /// Mark this entity as modified and notify the tracking system.
        /// </summary>
        protected virtual void MarkAsModified()
        {
            if (HasBeenModified) return;

            HasBeenModified = true;

            // Notify listeners
            OnModified?.Invoke(this);

            // Notify the chunk tracker about the modification
            var chunkTracker = SaveSystem.Instance?.ChunkTracker;
            if (chunkTracker != null && _rb != null && Definition != null)
            {
                chunkTracker.OnEntityModified(OriginChunk, Definition, _rb.linearVelocity, _rb.angularVelocity);
            }

            Debug.Log($"[{GetType().Name}] {EntityType} marked as modified in chunk ({OriginChunk.X}, {OriginChunk.Y})");
        }

        // ── Utility Methods ─────────────────────────────────────────────────

        /// <summary>
        /// Create the base SimulatedEntity with common properties.
        /// Derived classes should call this and then add type-specific data.
        /// </summary>
        protected SimulatedEntity CreateBaseSimulatedEntity()
        {
            var service = WorldGenerationService.Instance;
            var absolutePos = service != null
                ? service.WorldToAbsolute(transform.position)
                : Vector2D.FromVector2(transform.position);

            return new SimulatedEntity
            {
                EntityId = EntityId,
                EntityType = EntityType,
                CurrentChunk = CurrentChunk,
                OriginChunk = OriginChunk,
                AbsolutePosition = absolutePos,
                Velocity = _rb != null ? Vector2D.FromVector2(_rb.linearVelocity) : Vector2D.Zero,
                Rotation = _rb != null ? _rb.rotation : transform.rotation.eulerAngles.z,
                AngularVelocity = _rb != null ? _rb.angularVelocity : 0f,
                Mass = _rb != null ? _rb.mass : 1f,
                Radius = GetEntityRadius(),
                Drag = _rb != null ? _rb.linearDamping : 0f,
                HasBeenModified = true,
                InstigatorEntityId = _instigatorTracker?.InstigatorEntityId
            };
        }

        /// <summary>
        /// Get the radius of this entity for collision detection.
        /// Override in derived classes for more accurate radius calculation.
        /// </summary>
        protected virtual float GetEntityRadius()
        {
            // Try to get radius from a CircleCollider2D
            var circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                return circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
            }

            // Fallback: use half the size from sprite renderer
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var bounds = spriteRenderer.bounds;
                return Mathf.Max(bounds.extents.x, bounds.extents.y);
            }

            // Default fallback
            return 1f;
        }

        /// <summary>
        /// Generate a unique entity ID based on chunk origin and index.
        /// </summary>
        protected static int GenerateEntityId(ChunkCoord chunk, int index)
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash ^ chunk.X.GetHashCode()) * 16777619;
                hash = (hash ^ chunk.Y.GetHashCode()) * 16777619;
                hash = (hash ^ index) * 16777619;
                return hash;
            }
        }

        /// <summary>
        /// Get the current velocity of the entity.
        /// </summary>
        public Vector2 GetVelocity()
        {
            return _rb != null ? _rb.linearVelocity : Vector2.zero;
        }

        /// <summary>
        /// Get the current angular velocity of the entity.
        /// </summary>
        public float GetAngularVelocity()
        {
            return _rb != null ? _rb.angularVelocity : 0f;
        }

        /// <summary>
        /// Reset tracker state for pooling.
        /// </summary>
        public virtual void ResetTracker()
        {
            EntityId = 0;
            OriginChunk = default;
            CurrentChunk = default;
            HasBeenModified = false;
            _initialized = false;

            // Clear event subscriptions
            OnRequestDestroy = null;
            OnChunkMigration = null;
            OnModified = null;
        }
    }
}
