using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Core.V2.World.Generation;

namespace Starfire.Core.V2.World.Chunk
{
    /// <summary>
    /// Manages chunk loading, unloading, and spatial queries.
    /// Uses a dictionary for O(1) chunk lookup by coordinate.
    /// </summary>
    public class ChunkManager
    {
        private readonly Dictionary<ChunkCoord, Chunk> _loadedChunks = new Dictionary<ChunkCoord, Chunk>();
        private readonly HashSet<ChunkCoord> _chunksToLoad = new HashSet<ChunkCoord>();
        private readonly HashSet<ChunkCoord> _chunksToUnload = new HashSet<ChunkCoord>();
        private readonly List<ChunkCoord> _processingBuffer = new List<ChunkCoord>();

        private readonly float _chunkSize;
        private readonly float _worldSeed;

        /// <summary>
        /// Radius in chunks to keep loaded around the center position.
        /// </summary>
        public int LoadRadius { get; set; } = 3;

        /// <summary>
        /// Distance in chunks at which chunks begin unloading.
        /// Should be greater than LoadRadius to prevent thrashing.
        /// </summary>
        public int UnloadRadius { get; set; } = 5;

        /// <summary>
        /// Maximum number of chunks to keep loaded at once.
        /// </summary>
        public int MaxLoadedChunks { get; set; } = 100;

        /// <summary>
        /// Maximum chunks to process (load or unload) per update.
        /// </summary>
        public int ChunksPerFrame { get; set; } = 2;

        /// <summary>
        /// Fired when a chunk begins loading.
        /// </summary>
        public event Action<Chunk> OnChunkLoading;

        /// <summary>
        /// Fired when a chunk has finished loading.
        /// </summary>
        public event Action<Chunk> OnChunkLoaded;

        /// <summary>
        /// Fired when a chunk begins unloading.
        /// </summary>
        public event Action<Chunk> OnChunkUnloading;

        /// <summary>
        /// Fired when a chunk has finished unloading.
        /// </summary>
        public event Action<Chunk> OnChunkUnloaded;

        /// <summary>
        /// Current center chunk coordinate.
        /// </summary>
        public ChunkCoord CenterChunk { get; private set; }

        /// <summary>
        /// Number of currently loaded chunks.
        /// </summary>
        public int LoadedChunkCount => _loadedChunks.Count;

        public ChunkManager(float chunkSize, float worldSeed)
        {
            _chunkSize = chunkSize;
            _worldSeed = worldSeed;
        }

        /// <summary>
        /// Update chunk loading/unloading based on an absolute position.
        /// </summary>
        /// <param name="absolutePosition">Position in absolute space (not Unity shifted space)</param>
        public void UpdateAroundPosition(Vector2D absolutePosition)
        {
            ChunkCoord newCenter = ChunkCoord.FromAbsolutePosition(absolutePosition, _chunkSize);

            if (newCenter != CenterChunk || _loadedChunks.Count == 0)
            {
                CenterChunk = newCenter;
                DetermineChunksToLoad(newCenter);
                DetermineChunksToUnload(newCenter);
            }

            ProcessLoadQueue();
            ProcessUnloadQueue();
            EnforceChunkLimit();
        }

        /// <summary>
        /// Get chunk at coordinate, or null if not loaded.
        /// </summary>
        public Chunk GetChunk(ChunkCoord coord)
        {
            if (_loadedChunks.TryGetValue(coord, out var chunk))
            {
                chunk.Touch();
                return chunk;
            }
            return null;
        }

        /// <summary>
        /// Get chunk containing an absolute position, or null if not loaded.
        /// </summary>
        public Chunk GetChunkAtAbsolutePosition(Vector2D absolutePos)
        {
            ChunkCoord coord = ChunkCoord.FromAbsolutePosition(absolutePos, _chunkSize);
            return GetChunk(coord);
        }

        /// <summary>
        /// Get all currently loaded chunks.
        /// </summary>
        public IReadOnlyDictionary<ChunkCoord, Chunk> LoadedChunks => _loadedChunks;

        /// <summary>
        /// Get all chunks within a radius of a coordinate.
        /// Only returns loaded chunks.
        /// </summary>
        public IEnumerable<Chunk> GetChunksInRadius(ChunkCoord center, int radius)
        {
            foreach (var coord in center.GetCoordsInRadius(radius))
            {
                if (_loadedChunks.TryGetValue(coord, out var chunk))
                {
                    chunk.Touch();
                    yield return chunk;
                }
            }
        }

        /// <summary>
        /// Check if a chunk is loaded.
        /// </summary>
        public bool IsChunkLoaded(ChunkCoord coord)
        {
            return _loadedChunks.ContainsKey(coord);
        }

        /// <summary>
        /// Force immediate load of a specific chunk.
        /// Returns the loaded chunk.
        /// </summary>
        public Chunk ForceLoadChunk(ChunkCoord coord)
        {
            if (_loadedChunks.TryGetValue(coord, out var existing))
            {
                existing.Touch();
                return existing;
            }

            var chunk = CreateChunk(coord);
            LoadChunk(chunk);
            return chunk;
        }

        /// <summary>
        /// Force immediate unload of a specific chunk.
        /// </summary>
        public void ForceUnloadChunk(ChunkCoord coord)
        {
            if (_loadedChunks.TryGetValue(coord, out var chunk))
            {
                UnloadChunk(chunk);
            }
        }

        /// <summary>
        /// Unload all chunks.
        /// </summary>
        public void UnloadAllChunks()
        {
            _processingBuffer.Clear();
            _processingBuffer.AddRange(_loadedChunks.Keys);

            foreach (var coord in _processingBuffer)
            {
                if (_loadedChunks.TryGetValue(coord, out var chunk))
                {
                    UnloadChunk(chunk);
                }
            }

            _chunksToLoad.Clear();
            _chunksToUnload.Clear();
        }

        /// <summary>
        /// Get the chunk size.
        /// </summary>
        public float ChunkSize => _chunkSize;

        /// <summary>
        /// Get the world seed.
        /// </summary>
        public float WorldSeed => _worldSeed;

        private void DetermineChunksToLoad(ChunkCoord center)
        {
            _chunksToLoad.Clear();

            foreach (var coord in center.GetCoordsInRadius(LoadRadius))
            {
                if (!_loadedChunks.ContainsKey(coord))
                {
                    _chunksToLoad.Add(coord);
                }
            }
        }

        private void DetermineChunksToUnload(ChunkCoord center)
        {
            _chunksToUnload.Clear();

            foreach (var kvp in _loadedChunks)
            {
                int distance = kvp.Key.ChebyshevDistance(center);
                if (distance > UnloadRadius)
                {
                    _chunksToUnload.Add(kvp.Key);
                }
            }
        }

        private void ProcessLoadQueue()
        {
            if (_chunksToLoad.Count == 0) return;

            int processed = 0;
            _processingBuffer.Clear();
            _processingBuffer.AddRange(_chunksToLoad);

            // Sort by distance to center for prioritized loading
            _processingBuffer.Sort((a, b) =>
                a.ChebyshevDistance(CenterChunk).CompareTo(b.ChebyshevDistance(CenterChunk)));

            foreach (var coord in _processingBuffer)
            {
                if (processed >= ChunksPerFrame) break;

                var chunk = CreateChunk(coord);
                LoadChunk(chunk);
                _chunksToLoad.Remove(coord);
                processed++;
            }
        }

        private void ProcessUnloadQueue()
        {
            if (_chunksToUnload.Count == 0) return;

            int processed = 0;
            _processingBuffer.Clear();
            _processingBuffer.AddRange(_chunksToUnload);

            // Sort by distance (farthest first) and last access time
            _processingBuffer.Sort((a, b) =>
            {
                int distA = a.ChebyshevDistance(CenterChunk);
                int distB = b.ChebyshevDistance(CenterChunk);
                if (distA != distB) return distB.CompareTo(distA);

                // Secondary sort by last access time (oldest first)
                if (_loadedChunks.TryGetValue(a, out var chunkA) &&
                    _loadedChunks.TryGetValue(b, out var chunkB))
                {
                    return chunkA.LastAccessTime.CompareTo(chunkB.LastAccessTime);
                }
                return 0;
            });

            foreach (var coord in _processingBuffer)
            {
                if (processed >= ChunksPerFrame) break;

                if (_loadedChunks.TryGetValue(coord, out var chunk))
                {
                    UnloadChunk(chunk);
                    _chunksToUnload.Remove(coord);
                    processed++;
                }
            }
        }

        private void EnforceChunkLimit()
        {
            if (_loadedChunks.Count <= MaxLoadedChunks) return;

            // Find chunks to unload based on LRU
            _processingBuffer.Clear();
            foreach (var kvp in _loadedChunks)
            {
                // Don't unload chunks within load radius
                if (kvp.Key.ChebyshevDistance(CenterChunk) <= LoadRadius) continue;
                _processingBuffer.Add(kvp.Key);
            }

            // Sort by last access time (oldest first)
            _processingBuffer.Sort((a, b) =>
            {
                var timeA = _loadedChunks[a].LastAccessTime;
                var timeB = _loadedChunks[b].LastAccessTime;
                return timeA.CompareTo(timeB);
            });

            int toRemove = _loadedChunks.Count - MaxLoadedChunks;
            for (int i = 0; i < toRemove && i < _processingBuffer.Count; i++)
            {
                var coord = _processingBuffer[i];
                if (_loadedChunks.TryGetValue(coord, out var chunk))
                {
                    UnloadChunk(chunk);
                }
            }
        }

        private Chunk CreateChunk(ChunkCoord coord)
        {
            float seed = ChunkSeed.Calculate(_worldSeed, coord);
            return new Chunk(coord, _chunkSize, seed);
        }

        private void LoadChunk(Chunk chunk)
        {
            chunk.State = ChunkState.Loading;
            OnChunkLoading?.Invoke(chunk);

            _loadedChunks[chunk.Coord] = chunk;

            chunk.State = ChunkState.Loaded;
            chunk.Touch();
            OnChunkLoaded?.Invoke(chunk);
        }

        private void UnloadChunk(Chunk chunk)
        {
            chunk.State = ChunkState.Unloading;
            OnChunkUnloading?.Invoke(chunk);

            chunk.ClearAllData();
            _loadedChunks.Remove(chunk.Coord);

            chunk.State = ChunkState.Unloaded;
            OnChunkUnloaded?.Invoke(chunk);
        }
    }
}
