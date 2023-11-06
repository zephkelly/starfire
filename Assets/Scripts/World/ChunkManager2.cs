using System;
using System.Collections.Generic;
using UnityEngine;
using Starfire.Utils;
using Starfire.IO;

namespace Starfire
{
  //TODO: Combine chunks to only one Dictionary
  //TODO: Remove unnecessary edge conditions
  //TODO: Create universal GetChunk, ReleaseChunk methods to be called in the GetChunks method

  [RequireComponent(typeof(StarGenerator))]
  public class ChunkManager2 : MonoBehaviour
  {
    const int chunkDiameter = 300;
    private int maxOriginDistance = 3000;

    private StarGenerator starGenerator;

    private Transform cameraTransform;
    private Transform entityTransform;

    private Vector2 entityLastPosition;
    private Vector2Int entityWorldChunkPosition;
    private Vector2Int entityLastWorldChunkPosition;

    private Vector2D entityAbsolutePosition;
    private Vector2Int entityAbsoluteChunkPosition;
    private Vector2Int entityLastAbsoluteChunkPosition;

    private Dictionary<Vector2Int, Chunk> chunksDict = new Dictionary<Vector2Int, Chunk>();
    private ObjectPool<GameObject> chunkPool;
    private long chunkIndex = 0;

    private long ChunkIndex { get => chunkIndex++; }

    private void Awake()
    {
      starGenerator = GetComponent<StarGenerator>();
      cameraTransform = Camera.main.transform;
      entityTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
      chunkPool = new ObjectPool<GameObject>(() => 
      {
        return new GameObject("Chunk");
      }, _chunkObject => 
      {
        _chunkObject.SetActive(true);
      }, _chunkObject => 
      {
        _chunkObject.SetActive(false);S
      }, _chunkObject => 
      {
        Destroy(_chunkObject);
      }, false, 150, 200);

      GetEntityAbsolutePosition();
      GetEntityChunkPositions();
    }

    private void Update()
    {
      GetEntityAbsolutePosition();
      GetEntityChunkPositions();
    }

    private void LateUpdate()
    {
      SetLastPositions();
    }

    private void GetCurrentChunks()
    {
      Vector2Int _chunkAbsKey = new Vector2Int();
      Vector2Int _chunkWorldKey = new Vector2Int();

      for (int x = -5; x <= 5; x++)
      {
        for (int y = -5; y <= 5; y++)
        {
          _chunkAbsKey = GetChunkAbsKey(x, y);
          _chunkWorldKey = GetChunkWorldKey(x, y);

          //if chunksDict has chunk, set it active

          //else if (SaveManager has chunk available load it) then set it active

          //else make a new chunk and set it active

          SetActiveChunk(x, y);
        }
      }
    }

    private void SetActiveChunk(Chunk _chunk, int _x, int _y)
    {
      if (Math.Abs(_x) <= 3 && Math.Abs(_y) <= 3)
      {
        
      }
    }

    private Vector2Int GetChunkAbsKey(int x, int y)
    {
      return new Vector2Int(
        entityAbsoluteChunkPosition.x + x,
        entityAbsoluteChunkPosition.y + y
      );
    }

    private Vector2Int GetChunkWorldKey(int x, int y)
    {
      return new Vector2Int(
        entityWorldChunkPosition.x + x,
        entityWorldChunkPosition.y + y
      );
    }

    private void GetEntityAbsolutePosition()
    {
      entityAbsolutePosition.x += entityTransform.position.x - entityLastPosition.x;
      entityAbsolutePosition.y += entityTransform.position.y - entityLastPosition.y;
    }

    private void GetEntityChunkPositions()
    {
      entityAbsoluteChunkPosition = ChunkUtils.GetEntityChunkPosition(entityAbsolutePosition, chunkDiameter);
      entityWorldChunkPosition = ChunkUtils.GetEntityChunkPosition(entityTransform.position, chunkDiameter);
    }

    private void SetLastPositions()
    {
      entityLastPosition = entityTransform.position;
      entityLastAbsoluteChunkPosition = entityAbsoluteChunkPosition;
      entityLastWorldChunkPosition = entityWorldChunkPosition;
    }
  }
}