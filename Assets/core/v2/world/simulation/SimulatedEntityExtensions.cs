using System.Collections.Generic;
using Starfire.Core.V2.World;

namespace Starfire.Core.V2.World.Simulation
{
    /// <summary>
    /// Extension methods for type-specific SimulatedEntity data access.
    /// Provides a clean API for getting/setting entity-type-specific properties
    /// stored in the TypeData dictionary.
    /// </summary>
    public static class SimulatedEntityExtensions
    {
        // ── Asteroid Extensions ─────────────────────────────────────────────

        // Note: Asteroids use the legacy fields (Variant, Seed, SourceType) directly
        // for backward compatibility. These extension methods are provided for
        // consistency but delegate to the existing fields.

        public static int GetVariant(this SimulatedEntity entity)
            => entity.Variant;

        public static void SetVariant(this SimulatedEntity entity, int variant)
            => entity.Variant = variant;

        public static float GetSeed(this SimulatedEntity entity)
            => entity.Seed;

        public static void SetSeed(this SimulatedEntity entity, float seed)
            => entity.Seed = seed;

        public static int GetSourceType(this SimulatedEntity entity)
            => entity.SourceType;

        public static void SetSourceType(this SimulatedEntity entity, int sourceType)
            => entity.SourceType = sourceType;

        // ── Ship Extensions ─────────────────────────────────────────────────

        private const string KeyShipClassId = "ShipClassId";
        private const string KeyModulesJson = "ModulesJson";
        private const string KeyBehaviorSnapshot = "BehaviorSnapshot";
        private const string KeyFactionId = "FactionId";

        public static string GetShipClassId(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyShipClassId, out var v) ? (string)v : null;
        }

        public static void SetShipClassId(this SimulatedEntity entity, string classId)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyShipClassId] = classId;
        }

        public static string GetModulesJson(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyModulesJson, out var v) ? (string)v : null;
        }

        public static void SetModulesJson(this SimulatedEntity entity, string json)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyModulesJson] = json;
        }

        public static int GetFactionId(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyFactionId, out var v) ? (int)v : 0;
        }

        public static void SetFactionId(this SimulatedEntity entity, int factionId)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyFactionId] = factionId;
        }

        // ── Behavior Snapshot (Ships) ───────────────────────────────────────

        public static BehaviorSnapshot GetBehaviorSnapshot(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyBehaviorSnapshot, out var v)
                ? (BehaviorSnapshot)v
                : null;
        }

        public static void SetBehaviorSnapshot(this SimulatedEntity entity, BehaviorSnapshot snapshot)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyBehaviorSnapshot] = snapshot;
        }

        // ── Projectile Extensions ───────────────────────────────────────────

        private const string KeyFirerId = "FirerId";
        private const string KeyTargetId = "TargetId";
        private const string KeyProjectileType = "ProjectileType";
        private const string KeyDamage = "Damage";
        private const string KeyLifetimeRemaining = "LifetimeRemaining";

        public static int GetFirerId(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyFirerId, out var v) ? (int)v : -1;
        }

        public static void SetFirerId(this SimulatedEntity entity, int firerId)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyFirerId] = firerId;
        }

        public static int GetTargetId(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyTargetId, out var v) ? (int)v : -1;
        }

        public static void SetTargetId(this SimulatedEntity entity, int targetId)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyTargetId] = targetId;
        }

        public static int GetProjectileType(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyProjectileType, out var v) ? (int)v : 0;
        }

        public static void SetProjectileType(this SimulatedEntity entity, int projectileType)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyProjectileType] = projectileType;
        }

        public static float GetDamage(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyDamage, out var v) ? (float)v : 0f;
        }

        public static void SetDamage(this SimulatedEntity entity, float damage)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyDamage] = damage;
        }

        public static float GetLifetimeRemaining(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyLifetimeRemaining, out var v) ? (float)v : 0f;
        }

        public static void SetLifetimeRemaining(this SimulatedEntity entity, float lifetime)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyLifetimeRemaining] = lifetime;
        }

        // ── Ship Capabilities ──────────────────────────────────────────────────

        private const string KeyShipCapabilities = "ShipCapabilities";

        /// <summary>
        /// Get the extracted ship capabilities for this entity.
        /// Returns Default capabilities if not set.
        /// </summary>
        public static SimulatedShipCapabilities GetShipCapabilities(this SimulatedEntity entity)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(KeyShipCapabilities, out var v)
                ? (SimulatedShipCapabilities)v
                : SimulatedShipCapabilities.Default;
        }

        /// <summary>
        /// Store extracted ship capabilities on this entity.
        /// </summary>
        public static void SetShipCapabilities(this SimulatedEntity entity, SimulatedShipCapabilities capabilities)
        {
            entity.EnsureTypeData();
            entity.TypeData[KeyShipCapabilities] = capabilities;
        }

        /// <summary>
        /// Check if this entity has ship capabilities stored.
        /// </summary>
        public static bool HasShipCapabilities(this SimulatedEntity entity)
        {
            return entity.TypeData != null && entity.TypeData.ContainsKey(KeyShipCapabilities);
        }

        // ── Generic Type-Safe Accessors ─────────────────────────────────────

        public static T GetTypeData<T>(this SimulatedEntity entity, string key, T defaultValue = default)
        {
            entity.EnsureTypeData();
            return entity.TypeData.TryGetValue(key, out var v) && v is T typedValue
                ? typedValue
                : defaultValue;
        }

        public static void SetTypeData<T>(this SimulatedEntity entity, string key, T value)
        {
            entity.EnsureTypeData();
            entity.TypeData[key] = value;
        }

        public static bool HasTypeData(this SimulatedEntity entity, string key)
        {
            return entity.TypeData != null && entity.TypeData.ContainsKey(key);
        }

        public static void RemoveTypeData(this SimulatedEntity entity, string key)
        {
            entity.TypeData?.Remove(key);
        }
    }
}
