using UnityEngine;

namespace Starfire
{
    [RequireComponent(typeof(ChunkManager))]
    [RequireComponent(typeof(FloatingOriginSystem))]
    public class PlayerPositionService : MonoBehaviour, IPlayerPositionService
    {
        private Transform playerTransform;
        private Vector2 playerLastPosition;

        private Vector2D playerAbsolutePosition;
        private Vector2D playerLastAbsolutePosition;

        private Vector2Int playerAbsoluteChunkPosition;
        private Vector2Int playerLastAbsoluteChunkPosition;

        private Vector2Int playerWorldChunkPosition;
        private Vector2Int playerLastWorldChunkPosition;

        private void Awake()
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        private void Start()
        {
            playerLastPosition = playerTransform.position;

            UpdateAbsolutePosition();
            UpdateAbsoluteChunkPosition();
            UpdateWorldChunkPosition();
        }

        public Vector2D GetAbsolutePosition() => playerAbsolutePosition;

        public Vector2D GetLastAbsolutePosition() => playerLastAbsolutePosition;

        public Vector2Int GetAbsoluteChunkPosition() => playerAbsoluteChunkPosition;

        public Vector2Int GetLastAbsoluteChunkPosition() => playerLastAbsoluteChunkPosition;

        public Vector2Int GetWorldChunkPosition() => playerWorldChunkPosition;

        public Vector2Int GetLastWorldChunkPosition() => playerLastWorldChunkPosition;

        public void UpdateAbsolutePosition()
        {
            playerLastAbsolutePosition = playerAbsolutePosition;

            playerAbsolutePosition += new Vector2D
            (
                playerTransform.position.x - playerLastPosition.x,
                playerTransform.position.y - playerLastPosition.y
            );

            playerLastPosition = playerTransform.position;
        }

        public void UpdateAbsoluteChunkPosition()
        {
            playerLastAbsoluteChunkPosition = playerAbsoluteChunkPosition;

            playerAbsoluteChunkPosition = ChunkUtils.GetChunkPosition(playerAbsolutePosition, ChunkManager.Instance.ChunkDiameter);
        }

        public void UpdateWorldChunkPosition()
        {
            playerLastWorldChunkPosition = playerWorldChunkPosition;
            
            playerWorldChunkPosition = ChunkUtils.GetChunkPosition(playerTransform.position, ChunkManager.Instance.ChunkDiameter);
        }

        // public void UpdateLastPositions()
        // {
        //     playerLastPosition = playerTransform.position;
        //     playerLastAbsolutePosition = playerAbsolutePosition;
        //     playerLastAbsoluteChunkPosition = playerAbsoluteChunkPosition;
        //     playerLastWorldChunkPosition = playerWorldChunkPosition;
        // }

        public void ClearLastPositions()
        {
            playerLastPosition = playerTransform.position;
            playerLastAbsolutePosition = playerAbsolutePosition;
            playerLastAbsoluteChunkPosition = playerAbsoluteChunkPosition;
            playerLastWorldChunkPosition = playerWorldChunkPosition;
        }
    }
}