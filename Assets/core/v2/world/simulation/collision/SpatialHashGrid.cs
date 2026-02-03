using System;
using System.Collections.Generic;

namespace Starfire.Core.V2.World.Simulation.Collision
{
    /// <summary>
    /// Spatial hash grid for broad-phase collision detection.
    /// Uses double-precision coordinates and long cell indices for large world support.
    /// O(n) insertion, O(1) neighbor lookup.
    /// </summary>
    public class SpatialHashGrid
    {
        private readonly double _cellSize;
        private readonly Dictionary<(long, long), List<int>> _cells = new();
        private readonly List<int> _reusableList = new();

        public SpatialHashGrid(double cellSize)
        {
            _cellSize = cellSize;
        }

        public double CellSize => _cellSize;

        /// <summary>
        /// Clears all entities from the grid.
        /// </summary>
        public void Clear()
        {
            foreach (var cell in _cells.Values)
            {
                cell.Clear();
            }
        }

        /// <summary>
        /// Converts a world position to cell coordinates.
        /// </summary>
        public (long X, long Y) GetCellCoord(Vector2D position)
        {
            return (
                (long)Math.Floor(position.X / _cellSize),
                (long)Math.Floor(position.Y / _cellSize)
            );
        }

        /// <summary>
        /// Inserts an entity ID at the given position.
        /// </summary>
        public void Insert(int entityId, Vector2D position)
        {
            var cell = GetCellCoord(position);
            if (!_cells.TryGetValue(cell, out var list))
            {
                list = new List<int>();
                _cells[cell] = list;
            }
            list.Add(entityId);
        }

        /// <summary>
        /// Returns all entity IDs in the same cell and neighboring cells.
        /// The entity at the given position should filter itself out.
        /// </summary>
        public IReadOnlyList<int> GetPotentialColliders(Vector2D position, float radius)
        {
            _reusableList.Clear();

            var center = GetCellCoord(position);

            // Check how many cells the radius spans
            int cellRadius = (int)Math.Ceiling(radius / _cellSize);
            if (cellRadius < 1) cellRadius = 1;

            for (long dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (long dy = -cellRadius; dy <= cellRadius; dy++)
                {
                    var cell = (center.X + dx, center.Y + dy);
                    if (_cells.TryGetValue(cell, out var list))
                    {
                        _reusableList.AddRange(list);
                    }
                }
            }

            return _reusableList;
        }

        /// <summary>
        /// Returns all entity IDs that could potentially collide with an entity
        /// at the given position with the given radius. Checks the 3x3 neighborhood.
        /// </summary>
        public IReadOnlyList<int> GetNearbyEntities(Vector2D position)
        {
            _reusableList.Clear();

            var center = GetCellCoord(position);

            // Check 3x3 neighborhood
            for (long dx = -1; dx <= 1; dx++)
            {
                for (long dy = -1; dy <= 1; dy++)
                {
                    var cell = (center.X + dx, center.Y + dy);
                    if (_cells.TryGetValue(cell, out var list))
                    {
                        _reusableList.AddRange(list);
                    }
                }
            }

            return _reusableList;
        }

        /// <summary>
        /// Returns the number of non-empty cells (for debugging).
        /// </summary>
        public int ActiveCellCount
        {
            get
            {
                int count = 0;
                foreach (var cell in _cells.Values)
                {
                    if (cell.Count > 0) count++;
                }
                return count;
            }
        }
    }
}
