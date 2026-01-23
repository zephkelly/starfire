using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Core.V2.World.Sampling
{
    /// <summary>
    /// Poisson disk sampling for natural point distribution.
    /// Ensures minimum spacing between generated points.
    /// Uses Bridson's algorithm for efficiency.
    /// </summary>
    public static class PoissonDiskSampler
    {
        /// <summary>
        /// Generate points within bounds with minimum spacing.
        /// </summary>
        /// <param name="bounds">World-space bounds to fill</param>
        /// <param name="minDistance">Minimum distance between points</param>
        /// <param name="rng">Random number generator for determinism</param>
        /// <param name="maxAttempts">Attempts per active point (default 30)</param>
        /// <returns>List of generated points within bounds</returns>
        public static List<Vector2> Sample(
            Rect bounds,
            float minDistance,
            System.Random rng,
            int maxAttempts = 30)
        {
            var points = new List<Vector2>();
            var activeList = new List<int>();

            // Cell size for the spatial grid (r / sqrt(2) ensures at most one point per cell)
            float cellSize = minDistance / Mathf.Sqrt(2f);
            int gridWidth = Mathf.CeilToInt(bounds.width / cellSize);
            int gridHeight = Mathf.CeilToInt(bounds.height / cellSize);

            // Grid stores index into points list, or -1 if empty
            var grid = new int[gridWidth * gridHeight];
            for (int i = 0; i < grid.Length; i++)
                grid[i] = -1;

            // Start with a random initial point
            Vector2 initialPoint = new Vector2(
                bounds.x + (float)rng.NextDouble() * bounds.width,
                bounds.y + (float)rng.NextDouble() * bounds.height
            );

            AddPoint(initialPoint, points, activeList, grid, bounds, cellSize, gridWidth);

            // Process active list until empty
            while (activeList.Count > 0)
            {
                // Pick a random active point
                int activeIndex = rng.Next(activeList.Count);
                int pointIndex = activeList[activeIndex];
                Vector2 point = points[pointIndex];

                bool foundValid = false;

                // Try to find a valid point around this one
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Generate random point in annulus between r and 2r
                    float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                    float distance = minDistance + (float)rng.NextDouble() * minDistance;

                    Vector2 candidate = point + new Vector2(
                        Mathf.Cos(angle) * distance,
                        Mathf.Sin(angle) * distance
                    );

                    if (IsValidPoint(candidate, bounds, points, grid, minDistance, cellSize, gridWidth, gridHeight))
                    {
                        AddPoint(candidate, points, activeList, grid, bounds, cellSize, gridWidth);
                        foundValid = true;
                    }
                }

                // If no valid point found, remove from active list
                if (!foundValid)
                {
                    activeList.RemoveAt(activeIndex);
                }
            }

            return points;
        }

        /// <summary>
        /// Sample points with variable density based on a density function.
        /// </summary>
        /// <param name="bounds">World-space bounds to fill</param>
        /// <param name="minDistance">Minimum base distance between points</param>
        /// <param name="rng">Random number generator</param>
        /// <param name="densityFunc">Function returning density (0-1) at a point. Higher = more points.</param>
        /// <param name="maxAttempts">Attempts per active point</param>
        /// <returns>List of generated points</returns>
        public static List<Vector2> SampleWithDensity(
            Rect bounds,
            float minDistance,
            System.Random rng,
            System.Func<Vector2, float> densityFunc,
            int maxAttempts = 30)
        {
            var allPoints = Sample(bounds, minDistance, rng, maxAttempts);
            var filteredPoints = new List<Vector2>();

            foreach (var point in allPoints)
            {
                float density = Mathf.Clamp01(densityFunc(point));
                if ((float)rng.NextDouble() < density)
                {
                    filteredPoints.Add(point);
                }
            }

            return filteredPoints;
        }

        private static void AddPoint(
            Vector2 point,
            List<Vector2> points,
            List<int> activeList,
            int[] grid,
            Rect bounds,
            float cellSize,
            int gridWidth)
        {
            int pointIndex = points.Count;
            points.Add(point);
            activeList.Add(pointIndex);

            int gridX = Mathf.FloorToInt((point.x - bounds.x) / cellSize);
            int gridY = Mathf.FloorToInt((point.y - bounds.y) / cellSize);
            int gridIndex = gridY * gridWidth + gridX;

            if (gridIndex >= 0 && gridIndex < grid.Length)
            {
                grid[gridIndex] = pointIndex;
            }
        }

        private static bool IsValidPoint(
            Vector2 candidate,
            Rect bounds,
            List<Vector2> points,
            int[] grid,
            float minDistance,
            float cellSize,
            int gridWidth,
            int gridHeight)
        {
            // Check bounds
            if (!bounds.Contains(candidate))
                return false;

            // Get grid cell for candidate
            int gridX = Mathf.FloorToInt((candidate.x - bounds.x) / cellSize);
            int gridY = Mathf.FloorToInt((candidate.y - bounds.y) / cellSize);

            // Check neighboring cells (2 cells in each direction to cover minDistance)
            int searchRadius = 2;
            float minDistSq = minDistance * minDistance;

            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    int nx = gridX + dx;
                    int ny = gridY + dy;

                    if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                        continue;

                    int neighborIndex = ny * gridWidth + nx;
                    int pointIndex = grid[neighborIndex];

                    if (pointIndex >= 0)
                    {
                        Vector2 neighbor = points[pointIndex];
                        float distSq = (candidate - neighbor).sqrMagnitude;

                        if (distSq < minDistSq)
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
