using System;
using System.Collections.Generic;
using System.Linq;
using Starfire.Core.V2.World.Chunk;
using StarfireV2;

namespace Starfire.Core.V2.World.Simulation.Events
{
    /// <summary>
    /// Stores and indexes simulation events for querying.
    /// Supports spatial, temporal, and instigator-based queries.
    /// </summary>
    public class SimulationEventLog
    {
        private readonly List<SimulationEvent> _events = new();
        private readonly Dictionary<ChunkCoord, List<int>> _eventsByChunk = new();
        private readonly Dictionary<int, List<int>> _eventsByInstigator = new();
        private readonly int _maxEvents;
        private readonly float _retentionSeconds;

        public SimulationEventLog(int maxEvents = 1000, float retentionSeconds = 600f)
        {
            _maxEvents = maxEvents;
            _retentionSeconds = retentionSeconds;
        }

        /// <summary>Total number of events stored.</summary>
        public int EventCount => _events.Count;

        /// <summary>Number of collision events stored.</summary>
        public int CollisionCount => _events.Count(e => e.Type == SimulationEventType.Collision);

        /// <summary>Number of destruction events stored.</summary>
        public int DestructionCount => _events.Count(e => e.Type == SimulationEventType.Destruction);

        /// <summary>
        /// Records an event and updates indexes.
        /// </summary>
        public void RecordEvent(SimulationEvent evt)
        {
            int index = _events.Count;
            _events.Add(evt);

            // Index by chunk
            if (!_eventsByChunk.TryGetValue(evt.Chunk, out var chunkList))
            {
                chunkList = new List<int>();
                _eventsByChunk[evt.Chunk] = chunkList;
            }
            chunkList.Add(index);

            // Index by instigator (for collision and destruction events)
            int? instigatorId = GetInstigatorId(evt);
            if (instigatorId.HasValue)
            {
                if (!_eventsByInstigator.TryGetValue(instigatorId.Value, out var instigatorList))
                {
                    instigatorList = new List<int>();
                    _eventsByInstigator[instigatorId.Value] = instigatorList;
                }
                instigatorList.Add(index);
            }

            // Enforce max events limit
            if (_events.Count > _maxEvents)
            {
                PruneOldestEvents(_events.Count - _maxEvents);
            }
        }

        private int? GetInstigatorId(SimulationEvent evt)
        {
            return evt switch
            {
                CollisionEvent ce => ce.InstigatorEntityId,
                DestructionEvent de => de.InstigatorEntityId,
                _ => null
            };
        }

        /// <summary>
        /// Gets all events in a region (chunk + radius).
        /// </summary>
        public IEnumerable<SimulationEvent> GetEventsInRegion(ChunkCoord center, int chunkRadius, double sinceTime = 0)
        {
            for (long dx = -chunkRadius; dx <= chunkRadius; dx++)
            {
                for (long dy = -chunkRadius; dy <= chunkRadius; dy++)
                {
                    var chunk = new ChunkCoord(center.X + dx, center.Y + dy);
                    if (_eventsByChunk.TryGetValue(chunk, out var indices))
                    {
                        foreach (int idx in indices)
                        {
                            if (idx < _events.Count)
                            {
                                var evt = _events[idx];
                                if (evt.Timestamp >= sinceTime)
                                {
                                    yield return evt;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets all events since a given time.
        /// </summary>
        public IEnumerable<SimulationEvent> GetEventsSince(double time)
        {
            // Events are added chronologically, so we can binary search
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                if (_events[i].Timestamp >= time)
                {
                    yield return _events[i];
                }
                else
                {
                    break; // Events before this are older
                }
            }
        }

        /// <summary>
        /// Gets all events caused by a specific instigator.
        /// </summary>
        public IEnumerable<SimulationEvent> GetEventsByInstigator(int instigatorEntityId)
        {
            if (_eventsByInstigator.TryGetValue(instigatorEntityId, out var indices))
            {
                foreach (int idx in indices)
                {
                    if (idx < _events.Count)
                    {
                        yield return _events[idx];
                    }
                }
            }
        }

        /// <summary>
        /// Gets all collision events caused by a specific instigator with minimum intensity.
        /// </summary>
        public IEnumerable<CollisionEvent> GetCollisionsByInstigator(int instigatorEntityId, float minIntensity = 0f)
        {
            foreach (var evt in GetEventsByInstigator(instigatorEntityId))
            {
                if (evt is CollisionEvent ce && ce.Intensity >= minIntensity)
                {
                    yield return ce;
                }
            }
        }

        /// <summary>
        /// Gets all destruction events for a specific entity type.
        /// </summary>
        public IEnumerable<DestructionEvent> GetDestructionsByVictimType(EntityType type)
        {
            return _events
                .OfType<DestructionEvent>()
                .Where(de => de.DestroyedType == type);
        }

        /// <summary>
        /// Gets all collision events.
        /// </summary>
        public IEnumerable<CollisionEvent> GetAllCollisions()
        {
            return _events.OfType<CollisionEvent>();
        }

        /// <summary>
        /// Gets all destruction events.
        /// </summary>
        public IEnumerable<DestructionEvent> GetAllDestructions()
        {
            return _events.OfType<DestructionEvent>();
        }

        /// <summary>
        /// Removes events older than the retention period.
        /// Call periodically to bound memory usage.
        /// </summary>
        public void Prune(double currentTime)
        {
            double cutoffTime = currentTime - _retentionSeconds;

            int removeCount = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Timestamp < cutoffTime)
                    removeCount++;
                else
                    break;
            }

            if (removeCount > 0)
            {
                PruneOldestEvents(removeCount);
            }
        }

        private void PruneOldestEvents(int count)
        {
            if (count <= 0 || count > _events.Count)
                return;

            // Remove from main list
            _events.RemoveRange(0, count);

            // Rebuild indexes (simpler than updating them)
            RebuildIndexes();
        }

        private void RebuildIndexes()
        {
            _eventsByChunk.Clear();
            _eventsByInstigator.Clear();

            for (int i = 0; i < _events.Count; i++)
            {
                var evt = _events[i];

                // Chunk index
                if (!_eventsByChunk.TryGetValue(evt.Chunk, out var chunkList))
                {
                    chunkList = new List<int>();
                    _eventsByChunk[evt.Chunk] = chunkList;
                }
                chunkList.Add(i);

                // Instigator index
                int? instigatorId = GetInstigatorId(evt);
                if (instigatorId.HasValue)
                {
                    if (!_eventsByInstigator.TryGetValue(instigatorId.Value, out var instigatorList))
                    {
                        instigatorList = new List<int>();
                        _eventsByInstigator[instigatorId.Value] = instigatorList;
                    }
                    instigatorList.Add(i);
                }
            }
        }

        /// <summary>
        /// Clears all events.
        /// </summary>
        public void Clear()
        {
            _events.Clear();
            _eventsByChunk.Clear();
            _eventsByInstigator.Clear();
        }
    }
}
