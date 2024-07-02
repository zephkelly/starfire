using UnityEngine;

namespace Starfire
{
    public interface IChunk
    {
        long ChunkIndex { get; }
        Vector2Int ChunkKey { get; }
        Vector2Int CurrentWorldKey { get; }
        Vector2Int ChunkCellKey { get; }
        ChunkState ChunkState { get; }
        GameObject ChunkObject { get; }

        bool HasStar { get; }
        Vector2 GetStarPosition { get; }

        void AddStarToChunk(Star chunkStar);
        void SetActiveChunk(Vector2Int chunkKey);
        void SetLazyChunk(Vector2Int chunkKey);
        void SetInactiveChunk();
    }
}
