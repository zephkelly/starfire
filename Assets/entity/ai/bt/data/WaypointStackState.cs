using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Entity.AI.BT
{
    /// <summary>
    /// Manages a stack of waypoint sequences, allowing nested navigation.
    /// Ships can push new sequences (e.g., exploration sub-waypoints) and
    /// pop back to previous sequences when complete.
    /// </summary>
    [Serializable]
    public class WaypointStackState
    {
        /// <summary>
        /// The stack of waypoint sequences. Last element is the current active sequence.
        /// </summary>
        public List<WaypointSequence> Stack = new();

        /// <summary>
        /// Maximum nesting depth to prevent infinite recursion.
        /// </summary>
        public int MaxDepth = 5;

        /// <summary>
        /// Gets the currently active sequence (top of stack), or null if empty.
        /// </summary>
        public WaypointSequence Current => Stack.Count > 0 ? Stack[^1] : null;

        /// <summary>
        /// Current stack depth (1 = base patrol, 2+ = nested sequences).
        /// </summary>
        public int Depth => Stack.Count;

        /// <summary>
        /// Returns true if the stack is empty.
        /// </summary>
        public bool IsEmpty => Stack.Count == 0;

        /// <summary>
        /// Returns true if at maximum depth (can't push more sequences).
        /// </summary>
        public bool IsAtMaxDepth => Stack.Count >= MaxDepth;

        /// <summary>
        /// Returns true if currently in a sub-sequence (depth > 1).
        /// </summary>
        public bool IsInSubSequence => Stack.Count > 1;

        /// <summary>
        /// Gets the current target waypoint from the active sequence.
        /// </summary>
        public Vector2? GetCurrentTarget()
        {
            return Current?.CurrentTarget;
        }

        /// <summary>
        /// Gets the current index in the active sequence.
        /// </summary>
        public int GetCurrentIndex()
        {
            return Current?.CurrentIndex ?? 0;
        }

        /// <summary>
        /// Push a new waypoint sequence onto the stack.
        /// Returns true if successful, false if at max depth.
        /// </summary>
        public bool Push(WaypointSequence sequence)
        {
            if (sequence == null || IsAtMaxDepth)
                return false;

            Stack.Add(sequence);
            return true;
        }

        /// <summary>
        /// Push a new sequence from a list of waypoints.
        /// Returns true if successful, false if at max depth.
        /// </summary>
        public bool Push(List<Vector2> waypoints, WaypointTraversalMode mode = WaypointTraversalMode.Once, string label = "")
        {
            if (waypoints == null || waypoints.Count == 0 || IsAtMaxDepth)
                return false;

            var sequence = WaypointSequence.Create(waypoints, mode, label);
            Stack.Add(sequence);
            return true;
        }

        /// <summary>
        /// Pop the current sequence and return to the previous one.
        /// Returns the popped sequence, or null if at base level.
        /// </summary>
        public WaypointSequence Pop()
        {
            if (Stack.Count <= 1)
                return null; // Don't pop the base sequence

            var popped = Stack[^1];
            Stack.RemoveAt(Stack.Count - 1);
            return popped;
        }

        /// <summary>
        /// Advance the current sequence's index.
        /// Returns true if advanced, false if sequence is complete.
        /// </summary>
        public bool AdvanceIndex()
        {
            return Current?.Advance() ?? false;
        }

        /// <summary>
        /// Check if the current sequence is complete (Once mode at last waypoint).
        /// </summary>
        public bool IsCurrentSequenceComplete()
        {
            var current = Current;
            if (current == null)
                return true;

            return current.Mode == WaypointTraversalMode.Once && current.IsAtEnd;
        }

        /// <summary>
        /// Initialize with a base waypoint sequence using static positions.
        /// Clears any existing stack.
        /// </summary>
        public void Initialize(List<Vector2> baseWaypoints, WaypointTraversalMode mode = WaypointTraversalMode.Loop, string label = "patrol")
        {
            Stack.Clear();
            if (baseWaypoints != null && baseWaypoints.Count > 0)
            {
                Push(baseWaypoints, mode, label);
            }
        }

        /// <summary>
        /// Initialize with a base waypoint sequence using dynamic Transform references.
        /// Clears any existing stack.
        /// </summary>
        public void Initialize(List<Transform> baseTransforms, WaypointTraversalMode mode = WaypointTraversalMode.Loop, string label = "patrol")
        {
            Stack.Clear();
            if (baseTransforms != null && baseTransforms.Count > 0)
            {
                var sequence = WaypointSequence.CreateDynamic(baseTransforms, mode, label);
                Stack.Add(sequence);
            }
        }

        /// <summary>
        /// Get the base (bottom) sequence, or null if empty.
        /// </summary>
        public WaypointSequence GetBaseSequence()
        {
            return Stack.Count > 0 ? Stack[0] : null;
        }
    }
}
