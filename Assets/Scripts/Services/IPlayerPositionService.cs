using UnityEngine;

namespace Starfire
{
    public interface IPlayerPositionService
    {
        Vector2D GetAbsolutePosition();
        Vector2D GetLastAbsolutePosition();

        Vector2Int GetAbsoluteChunkPosition();
        Vector2Int GetLastAbsoluteChunkPosition();

        Vector2Int GetWorldChunkPosition();
        Vector2Int GetLastWorldChunkPosition();

        void UpdateAbsolutePosition();
        void UpdateAbsoluteChunkPosition();
        void UpdateWorldChunkPosition();
        void ClearLastPositions();
    }
}