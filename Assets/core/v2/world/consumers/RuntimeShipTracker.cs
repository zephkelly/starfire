using UnityEngine;
using Starfire.Core.V2.Save;
using Starfire.Core.V2.World.Chunk;
using Starfire.Core.V2.World.Simulation;
using StarfireV2;

namespace Starfire.Core.V2.World.Consumers
{
    /// <summary>
    /// Tracks the runtime state of a spawned ship.
    /// Monitors chunk boundary crossings and handles transfer to background simulation.
    /// Extracts module capabilities when entering simulation to preserve ship behavior.
    /// </summary>
    [RequireComponent(typeof(ShipController))]
    public class RuntimeShipTracker : RuntimeEntityTrackerBase
    {
        // ── IRuntimeEntityTracker Implementation ────────────────────────────

        public override EntityType EntityType => EntityType.Ship;

        public override ISimulatableDefinition Definition => null; // Ships don't have procedural definitions yet

        // ── Ship-Specific State ─────────────────────────────────────────────

        private ShipController _shipController;

        /// <summary>The ship's class ID for reconstruction.</summary>
        public string ShipClassId { get; private set; }

        /// <summary>The ship's faction for allegiance tracking.</summary>
        public int FactionId { get; set; }

        // ── Unity Lifecycle ─────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _shipController = GetComponent<ShipController>();
        }

        // ── Initialization ──────────────────────────────────────────────────

        /// <summary>
        /// Initialize the tracker with spawn context.
        /// Call this after the ship GameObject is created.
        /// </summary>
        /// <param name="originChunk">The chunk where this ship was spawned.</param>
        /// <param name="entityId">Unique identifier for this ship.</param>
        /// <param name="shipClassId">The ship's class/configuration ID.</param>
        /// <param name="factionId">The ship's faction ID.</param>
        public void Initialize(ChunkCoord originChunk, int entityId, string shipClassId = null, int factionId = 0)
        {
            OriginChunk = originChunk;
            CurrentChunk = originChunk;
            EntityId = entityId;
            ShipClassId = shipClassId;
            FactionId = factionId;
            HasBeenModified = false;
            _initialized = true;
        }

        /// <summary>
        /// Initialize the tracker for a promoted (previously simulated) ship.
        /// </summary>
        public void InitializeFromSimulated(SimulatedEntity simEntity)
        {
            if (simEntity == null) return;

            OriginChunk = simEntity.OriginChunk;
            CurrentChunk = simEntity.CurrentChunk;
            EntityId = simEntity.EntityId;
            ShipClassId = simEntity.GetShipClassId();
            FactionId = simEntity.GetFactionId();
            HasBeenModified = true; // Already modified if coming from simulation
            _initialized = true;

            // Restore capabilities if available
            if (simEntity.HasShipCapabilities() && _shipController?.Ship != null)
            {
                var caps = simEntity.GetShipCapabilities();
                RestoreCapabilitiesToShip(caps);
            }
        }

        // ── Override Base Class Methods ─────────────────────────────────────

        public override SimulatedEntity ToSimulatedEntity()
        {
            var entity = CreateBaseSimulatedEntity();

            // Add ship-specific metadata
            entity.SetShipClassId(ShipClassId);
            entity.SetFactionId(FactionId);

            // Extract and store module capabilities
            if (_shipController?.Ship != null)
            {
                var capabilities = ShipCapabilitiesExtractor.Extract(_shipController.Ship);
                entity.SetShipCapabilities(capabilities);

                Debug.Log($"[RuntimeShipTracker] Extracted capabilities for ship {EntityId}: " +
                          $"Shield={capabilities.CurrentShield}/{capabilities.MaxShield}, " +
                          $"Hull={capabilities.CurrentIntegrity}/{capabilities.MaxIntegrity}, " +
                          $"Weapons={capabilities.WeaponCount} (DPS={capabilities.TotalDamagePerSecond:F1})");
            }

            // Create behavior snapshot from current AI state if applicable
            var behaviorSnapshot = CreateBehaviorSnapshot();
            if (behaviorSnapshot != null)
            {
                entity.SetBehaviorSnapshot(behaviorSnapshot);
            }

            return entity;
        }

        // ── Ship-Specific Helpers ───────────────────────────────────────────

        /// <summary>
        /// Create a behavior snapshot capturing the ship's current AI state.
        /// </summary>
        private BehaviorSnapshot CreateBehaviorSnapshot()
        {
            var ship = _shipController?.Ship;
            if (ship == null) return BehaviorSnapshot.Ballistic();

            var snapshot = new BehaviorSnapshot
            {
                Type = SimulatedBehaviorType.Ballistic // Default to ballistic
            };

            // Populate movement capabilities from modules
            var propulsion = ship.Propulsion;
            if (propulsion != null)
            {
                snapshot.MaxSpeed = propulsion.MaxSpeed;
                snapshot.Acceleration = propulsion.Acceleration;
            }

            var rotation = ship.Rotation;
            if (rotation != null)
            {
                snapshot.TurnRate = rotation.RotationSpeed;
            }

            // TODO: Extract actual behavior from AI driver if present
            // var aiDriver = _shipController.DriverStack.Find<AIEntityControllerDriver>();
            // if (aiDriver != null) { ... extract target, waypoints, etc. }

            return snapshot;
        }

        /// <summary>
        /// Restore capabilities to the ship after promotion from simulation.
        /// Updates module state to match what was stored during simulation.
        /// </summary>
        private void RestoreCapabilitiesToShip(SimulatedShipCapabilities caps)
        {
            var ship = _shipController?.Ship;
            if (ship == null) return;

            // Restore shield state
            var shield = ship.Shield;
            if (shield != null)
            {
                shield.CurrentShield = Mathf.RoundToInt(caps.CurrentShield);
            }

            // Restore hull state
            var hull = ship.Hull;
            if (hull != null)
            {
                hull.CurrentIntegrity = caps.CurrentIntegrity;
            }

            Debug.Log($"[RuntimeShipTracker] Restored capabilities for ship {EntityId}: " +
                      $"Shield={caps.CurrentShield}, Hull={caps.CurrentIntegrity}");
        }

        public override void ResetTracker()
        {
            base.ResetTracker();
            ShipClassId = null;
            FactionId = 0;
        }
    }
}
