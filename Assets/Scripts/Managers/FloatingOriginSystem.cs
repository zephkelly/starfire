using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Starfire
{
    [RequireComponent(typeof(ChunkManager))]
    [RequireComponent(typeof(PlayerPositionService))]
    public class FloatingOriginSystem : MonoBehaviour
    {
        private Transform playerTransform;
        private ChunkManager chunkManager;
        private PlayerPositionService playerPositionService;

        [SerializeField] private float floatingOriginLimit = 2400f;

        public UnityEvent<Vector2> OnFloatingOrigin;

        private void Awake()
        {
            chunkManager = GetComponent<ChunkManager>();
            playerPositionService = GetComponent<PlayerPositionService>();
        }

        private void Start()
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        private void Update()
        {
            if (playerTransform.position.magnitude > floatingOriginLimit)
            {
                Vector2 offset = -(Vector2)playerTransform.position;
                Vector2Int playerChunk = playerPositionService.GetAbsoluteChunkPosition();
                Vector2 chunkCenter = chunkManager.ChunksDict[playerChunk].ChunkObject.transform.position;
                Vector2 playerPosition = playerTransform.position;
                Vector2 distanceFromCenter = playerPosition - chunkCenter;
                offset += distanceFromCenter;
                
                OnFloatingOrigin.Invoke(offset);

                playerPositionService.ClearLastPositions();
            }

            playerPositionService.UpdateAbsolutePosition();
            playerPositionService.UpdateAbsoluteChunkPosition();
            playerPositionService.UpdateWorldChunkPosition();
        }

        private void LateUpdate()
        {
            
        }
    }
}
