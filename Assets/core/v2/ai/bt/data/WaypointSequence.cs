using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// A single sequence of waypoints with its own index and traversal mode.
    /// Used as a layer in the WaypointStackState.
    /// </summary>
    [Serializable]
    public class WaypointSequence
    {
        /// <summary>
        /// The waypoints in this sequence (static positions).
        /// </summary>
        public List<Vector2> Waypoints = new();

        /// <summary>
        /// Optional Transform references for dynamic waypoint tracking.
        /// When set, takes priority over Waypoints for position lookups.
        /// </summary>
        public List<Transform> Transforms = null;

        /// <summary>
        /// Current position in the waypoint list.
        /// </summary>
        public int CurrentIndex = 0;

        /// <summary>
        /// How to traverse this sequence (Loop, PingPong, Once).
        /// </summary>
        public WaypointTraversalMode Mode = WaypointTraversalMode.Once;

        /// <summary>
        /// Direction for PingPong mode (1 = forward, -1 = backward).
        /// </summary>
        public int Direction = 1;

        /// <summary>
        /// Debug label for this sequence (e.g., "patrol", "investigate", "scan_pattern").
        /// </summary>
        public string Label = "";

        /// <summary>
        /// Returns true if this sequence uses dynamic Transform tracking.
        /// </summary>
        public bool IsDynamic => Transforms != null && Transforms.Count > 0;

        /// <summary>
        /// Gets the number of waypoints in this sequence.
        /// </summary>
        public int Count => IsDynamic ? Transforms.Count : (Waypoints?.Count ?? 0);

        /// <summary>
        /// Gets the current target waypoint, or null if sequence is empty.
        /// When Transforms are set, reads live position from Transform.
        /// </summary>
        public Vector2? CurrentTarget
        {
            get
            {
                // Dynamic: read live position from Transform
                if (IsDynamic)
                {
                    int index = Mathf.Clamp(CurrentIndex, 0, Transforms.Count - 1);
                    var t = Transforms[index];
                    return t != null ? (Vector2)t.position : null;
                }

                // Static: use Vector2 list
                if (Waypoints == null || Waypoints.Count == 0)
                    return null;

                int staticIndex = Mathf.Clamp(CurrentIndex, 0, Waypoints.Count - 1);
                return Waypoints[staticIndex];
            }
        }

        /// <summary>
        /// Returns true if at the last waypoint (for Once mode completion check).
        /// </summary>
        public bool IsAtEnd => Count > 0 && CurrentIndex >= Count - 1;

        /// <summary>
        /// Returns true if at the first waypoint.
        /// </summary>
        public bool IsAtStart => CurrentIndex <= 0;

        /// <summary>
        /// Advances the index according to the traversal mode.
        /// Returns true if advanced successfully, false if sequence is complete (Once mode at end).
        /// </summary>
        public bool Advance()
        {
            int count = Count;
            if (count == 0)
                return false;

            switch (Mode)
            {
                case WaypointTraversalMode.Loop:
                    CurrentIndex = (CurrentIndex + 1) % count;
                    return true;

                case WaypointTraversalMode.PingPong:
                    CurrentIndex += Direction;
                    if (CurrentIndex >= count - 1)
                    {
                        CurrentIndex = count - 1;
                        Direction = -1;
                    }
                    else if (CurrentIndex <= 0)
                    {
                        CurrentIndex = 0;
                        Direction = 1;
                    }
                    return true;

                case WaypointTraversalMode.Once:
                    if (CurrentIndex < count - 1)
                    {
                        CurrentIndex++;
                        return true;
                    }
                    return false; // Sequence complete

                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates a new sequence from a list of static waypoints.
        /// </summary>
        public static WaypointSequence Create(
            List<Vector2> waypoints,
            WaypointTraversalMode mode = WaypointTraversalMode.Once,
            string label = "")
        {
            return new WaypointSequence
            {
                Waypoints = waypoints ?? new List<Vector2>(),
                Transforms = null,
                CurrentIndex = 0,
                Mode = mode,
                Direction = 1,
                Label = label
            };
        }

        /// <summary>
        /// Creates a new sequence from a list of Transform references for dynamic tracking.
        /// </summary>
        public static WaypointSequence CreateDynamic(
            List<Transform> transforms,
            WaypointTraversalMode mode = WaypointTraversalMode.Once,
            string label = "")
        {
            return new WaypointSequence
            {
                Waypoints = new List<Vector2>(),
                Transforms = transforms ?? new List<Transform>(),
                CurrentIndex = 0,
                Mode = mode,
                Direction = 1,
                Label = label
            };
        }
    }
}
