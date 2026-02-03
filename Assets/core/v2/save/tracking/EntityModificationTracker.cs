using System.Collections.Generic;
using StarfireV2;

namespace Starfire.Core.V2.Save.Tracking
{
    public class TrackedEntity
    {
        public int EntityId;
        public bool IsProcedural;
        public EntityModificationFlags Flags;
    }

    public class EntityModificationTracker
    {
        private readonly Dictionary<int, TrackedEntity> _tracked = new();

        public void Register(int entityId, bool isProcedural)
        {
            if (_tracked.ContainsKey(entityId)) return;

            _tracked[entityId] = new TrackedEntity
            {
                EntityId = entityId,
                IsProcedural = isProcedural,
                Flags = isProcedural ? EntityModificationFlags.None : EntityModificationFlags.SpawnedByPlayer
            };
        }

        public void MarkModified(int entityId, EntityModificationFlags flags)
        {
            if (_tracked.TryGetValue(entityId, out var tracked))
            {
                tracked.Flags |= flags;
            }
        }

        public void MarkDestroyed(int entityId)
        {
            if (_tracked.TryGetValue(entityId, out var tracked))
            {
                tracked.Flags |= EntityModificationFlags.Destroyed;
            }
        }

        public bool ShouldSave(int entityId)
        {
            if (!_tracked.TryGetValue(entityId, out var tracked))
                return false;

            // Save non-procedural entities always, procedural only if modified
            return !tracked.IsProcedural || tracked.Flags != EntityModificationFlags.None;
        }

        public EntityModificationFlags GetFlags(int entityId)
        {
            return _tracked.TryGetValue(entityId, out var tracked) ? tracked.Flags : EntityModificationFlags.None;
        }

        public void Unregister(int entityId) => _tracked.Remove(entityId);

        public IEnumerable<TrackedEntity> GetAllTracked() => _tracked.Values;

        public void Clear() => _tracked.Clear();
    }
}
