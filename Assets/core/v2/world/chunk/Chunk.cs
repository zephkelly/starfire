using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Data;

namespace Starfire.Core.V2.World.Chunk
{
    /// <summary>
    /// Represents a single chunk in the world grid.
    /// Contains all generated data for this chunk region.
    /// </summary>
    public class Chunk
    {
        /// <summary>
        /// Grid coordinates of this chunk.
        /// </summary>
        public ChunkCoord Coord { get; }

        /// <summary>
        /// Current lifecycle state of the chunk.
        /// </summary>
        public ChunkState State { get; internal set; }

        /// <summary>
        /// World-space bounds of this chunk.
        /// Note: This is in absolute space, not shifted Unity space.
        /// </summary>
        public Rect AbsoluteBounds { get; }

        /// <summary>
        /// Deterministic seed for this chunk's generation.
        /// Calculated from world seed and chunk coordinates.
        /// </summary>
        public float GenerationSeed { get; }

        /// <summary>
        /// Time (Time.time) when this chunk was last accessed.
        /// Used for LRU-based unloading decisions.
        /// </summary>
        public float LastAccessTime { get; internal set; }

        /// <summary>
        /// Size of this chunk in world units.
        /// </summary>
        public float ChunkSize { get; }

        // Data storage - keyed by data type for extensibility
        private readonly Dictionary<Type, ChunkData> _data = new Dictionary<Type, ChunkData>();

        public Chunk(ChunkCoord coord, float chunkSize, float generationSeed)
        {
            Coord = coord;
            ChunkSize = chunkSize;
            GenerationSeed = generationSeed;
            State = ChunkState.Unloaded;
            LastAccessTime = Time.time;
            AbsoluteBounds = coord.ToWorldBounds(chunkSize);
        }

        /// <summary>
        /// Store data of a specific type in this chunk.
        /// </summary>
        public void SetData<T>(T data) where T : ChunkData
        {
            _data[typeof(T)] = data;
        }

        /// <summary>
        /// Get data of a specific type from this chunk.
        /// Returns null if not found.
        /// </summary>
        public T GetData<T>() where T : ChunkData
        {
            if (_data.TryGetValue(typeof(T), out var data))
            {
                return data as T;
            }
            return null;
        }

        /// <summary>
        /// Check if this chunk has data of a specific type.
        /// </summary>
        public bool HasData<T>() where T : ChunkData
        {
            return _data.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Check if this chunk has data of a specific type (non-generic).
        /// </summary>
        public bool HasData(Type dataType)
        {
            return _data.ContainsKey(dataType);
        }

        /// <summary>
        /// Get data by type (non-generic).
        /// </summary>
        public ChunkData GetData(Type dataType)
        {
            _data.TryGetValue(dataType, out var data);
            return data;
        }

        /// <summary>
        /// Remove data of a specific type from this chunk.
        /// </summary>
        public void ClearData<T>() where T : ChunkData
        {
            if (_data.TryGetValue(typeof(T), out var data))
            {
                data.OnUnload();
                _data.Remove(typeof(T));
            }
        }

        /// <summary>
        /// Remove all data from this chunk.
        /// </summary>
        public void ClearAllData()
        {
            foreach (var data in _data.Values)
            {
                data.OnUnload();
            }
            _data.Clear();
        }

        /// <summary>
        /// Get all data types stored in this chunk.
        /// </summary>
        public IEnumerable<Type> GetDataTypes()
        {
            return _data.Keys;
        }

        /// <summary>
        /// Get all data stored in this chunk.
        /// </summary>
        public IEnumerable<ChunkData> GetAllData()
        {
            return _data.Values;
        }

        /// <summary>
        /// Get the center of this chunk in absolute space.
        /// </summary>
        public Vector2 GetAbsoluteCenter()
        {
            return AbsoluteBounds.center;
        }

        /// <summary>
        /// Mark this chunk as accessed (updates LastAccessTime).
        /// </summary>
        public void Touch()
        {
            LastAccessTime = Time.time;
        }

        public override string ToString()
        {
            return $"Chunk[{Coord}, State={State}, DataTypes={_data.Count}]";
        }
    }
}
