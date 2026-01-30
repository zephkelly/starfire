using System;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Core.V2.World.Chunk
{
    /// <summary>
    /// Immutable value type representing chunk coordinates in the infinite world grid.
    /// Provides efficient hashing and equality for dictionary keys.
    /// </summary>
    public readonly struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public readonly long X;
        public readonly long Y;

        public ChunkCoord(long x, long y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Convert a world position to chunk coordinates.
        /// </summary>
        /// <param name="worldPos">Position in world space</param>
        /// <param name="chunkSize">Size of each chunk in world units</param>
        public static ChunkCoord FromWorldPosition(Vector2 worldPos, float chunkSize)
        {
            long x = (long)Math.Floor(worldPos.x / chunkSize);
            long y = (long)Math.Floor(worldPos.y / chunkSize);
            return new ChunkCoord(x, y);
        }

        /// <summary>
        /// Convert an absolute position (double-precision) to chunk coordinates.
        /// </summary>
        /// <param name="absolutePos">Position in absolute space</param>
        /// <param name="chunkSize">Size of each chunk in world units</param>
        public static ChunkCoord FromAbsolutePosition(Vector2D absolutePos, double chunkSize)
        {
            long x = (long)Math.Floor(absolutePos.X / chunkSize);
            long y = (long)Math.Floor(absolutePos.Y / chunkSize);
            return new ChunkCoord(x, y);
        }

        /// <summary>
        /// Get the world-space center point of this chunk.
        /// </summary>
        /// <param name="chunkSize">Size of each chunk in world units</param>
        public Vector2 ToWorldCenter(float chunkSize)
        {
            return new Vector2(
                (X + 0.5f) * chunkSize,
                (Y + 0.5f) * chunkSize
            );
        }

        /// <summary>
        /// Get the absolute-space center point of this chunk (double-precision).
        /// </summary>
        /// <param name="chunkSize">Size of each chunk in world units</param>
        public Vector2D ToAbsoluteCenter(double chunkSize)
        {
            return new Vector2D(
                (X + 0.5) * chunkSize,
                (Y + 0.5) * chunkSize
            );
        }

        /// <summary>
        /// Get the world-space bounds of this chunk.
        /// </summary>
        /// <param name="chunkSize">Size of each chunk in world units</param>
        public Rect ToWorldBounds(float chunkSize)
        {
            return new Rect(
                X * chunkSize,
                Y * chunkSize,
                chunkSize,
                chunkSize
            );
        }

        /// <summary>
        /// Get the minimum corner of this chunk in world space.
        /// </summary>
        /// <param name="chunkSize">Size of each chunk in world units</param>
        public Vector2 ToWorldMin(float chunkSize)
        {
            return new Vector2(X * chunkSize, Y * chunkSize);
        }

        /// <summary>
        /// Get all 8 neighboring chunk coordinates (orthogonal and diagonal).
        /// </summary>
        public IEnumerable<ChunkCoord> GetNeighbors()
        {
            yield return new ChunkCoord(X - 1, Y + 1);
            yield return new ChunkCoord(X, Y + 1);
            yield return new ChunkCoord(X + 1, Y + 1);
            yield return new ChunkCoord(X - 1, Y);
            yield return new ChunkCoord(X + 1, Y);
            yield return new ChunkCoord(X - 1, Y - 1);
            yield return new ChunkCoord(X, Y - 1);
            yield return new ChunkCoord(X + 1, Y - 1);
        }

        /// <summary>
        /// Get chunk coordinates within a radius (Manhattan distance).
        /// </summary>
        /// <param name="radius">Radius in chunks</param>
        public IEnumerable<ChunkCoord> GetCoordsInRadius(int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    yield return new ChunkCoord(X + dx, Y + dy);
                }
            }
        }

        /// <summary>
        /// Calculate Manhattan distance to another chunk coordinate.
        /// </summary>
        public long ManhattanDistance(ChunkCoord other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        /// <summary>
        /// Calculate Chebyshev distance (max of x/y difference) to another chunk coordinate.
        /// </summary>
        public long ChebyshevDistance(ChunkCoord other)
        {
            return Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
        }

        public bool Equals(ChunkCoord other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // Delegate to long's GetHashCode for proper distribution at large coords
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(ChunkCoord a, ChunkCoord b) => a.Equals(b);
        public static bool operator !=(ChunkCoord a, ChunkCoord b) => !a.Equals(b);
        public static ChunkCoord operator +(ChunkCoord a, ChunkCoord b) => new ChunkCoord(a.X + b.X, a.Y + b.Y);
        public static ChunkCoord operator -(ChunkCoord a, ChunkCoord b) => new ChunkCoord(a.X - b.X, a.Y - b.Y);

        public override string ToString() => $"Chunk({X}, {Y})";
    }
}
