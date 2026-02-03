using System;
using Starfire.Core.V2.World.Chunk;

namespace Starfire.Core.V2.World.Simulation.Events
{
    /// <summary>
    /// Base class for all simulation events.
    /// Events are recorded when significant things happen in the background simulation.
    /// </summary>
    [Serializable]
    public class SimulationEvent
    {
        private static long _nextEventId = 1;

        /// <summary>Unique identifier for this event.</summary>
        public long EventId;

        /// <summary>Game time when the event occurred (Time.timeAsDouble).</summary>
        public double Timestamp;

        /// <summary>Type of event.</summary>
        public SimulationEventType Type;

        /// <summary>Absolute world position where the event occurred.</summary>
        public Vector2D Position;

        /// <summary>Chunk where the event occurred (for spatial queries).</summary>
        public ChunkCoord Chunk;

        /// <summary>
        /// Human-readable summary for narrative/quest systems.
        /// Example: "Asteroid collided with Station Alpha"
        /// </summary>
        public string NarrativeSummary;

        protected SimulationEvent()
        {
            EventId = _nextEventId++;
        }

        /// <summary>
        /// Resets the event ID counter. Call this when loading a save game.
        /// </summary>
        public static void ResetEventIdCounter(long startFrom = 1)
        {
            _nextEventId = startFrom;
        }

        /// <summary>
        /// Gets the next event ID that will be assigned.
        /// </summary>
        public static long NextEventId => _nextEventId;
    }
}
