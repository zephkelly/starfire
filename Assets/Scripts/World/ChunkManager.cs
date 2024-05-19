using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Starfire.Utils;

namespace Starfire
{
  [RequireComponent(typeof(StarGenerator))]
  public class ChunkManager : MonoBehaviour
  {
    public static ChunkManager Instance { get; private set; }
    private StarGenerator starGenerator;

    const int chunkDiameter = 400;
    // private int maxOriginDistance = 3000;

    private Transform cameraTransform;
    private Transform entityTransform;

    private Vector2 entityLastPosition;
    private Vector2Int entityWorldChunkPosition;
    private Vector2Int entityLastWorldChunkPosition;

    private Vector2D entityAbsolutePosition;
    private Vector2Int entityAbsoluteChunkPosition;
    private Vector2Int entityLastAbsoluteChunkPosition;

    private Dictionary<Vector2Int, Chunk> chunksDict = new Dictionary<Vector2Int, Chunk>();
    // private Dictionary<Vector2Int, Chunk> currentActiveChunks = new Dictionary<Vector2Int, Chunk>();
    private Dictionary<Vector2Int, Chunk> lastCurrentChunks = new Dictionary<Vector2Int, Chunk>();
    private ObjectPool<GameObject> chunkPool;
    private long chunkIndex = 0;

    public ChunkManager()
    {
      if (Instance == null) {
        Instance = this;
      } else {
        Destroy(gameObject);
      }
    }

    public Dictionary<Vector2Int, Chunk> ChunksDict { get => chunksDict; }
    public ObjectPool<GameObject> ChunkPool { get => chunkPool; }
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
        _chunkObject.SetActive(false);
      }, _chunkObject => 
      {
        Destroy(_chunkObject);
      }, false, 150, 200);

      GetEntityAbsolutePosition();
      GetEntityChunkPositions();

      GetCurrentChunks();
    }

    private void Update()
    {
        GetEntityAbsolutePosition();
        GetEntityChunkPositions();

        if (entityAbsoluteChunkPosition != entityLastAbsoluteChunkPosition)
        {
            GetCurrentChunks();
            MarkChunksInactive();
        }
    }

    private void LateUpdate()
    {
      SetLastPositions();
    }

    private void GetCurrentChunks()
    {
        Vector2Int _chunkAbsKey;
        Vector2Int _chunkWorldKey;

        for (int x = -7; x <= 7; x++)
        {
            for (int y = -7; y <= 7; y++)
            {
                _chunkAbsKey = GetChunkAbsKey(x, y);    //For searching dictionary
                _chunkWorldKey = GetChunkWorldKey(x, y);    //For placing chunk in world

                Chunk _chunk = null;

                if (chunksDict.ContainsKey(_chunkAbsKey))
                {
                    _chunk = chunksDict[_chunkAbsKey];
                    SetChunkState(_chunk, x, y);
                    // _currentActiveChunks.Add(_chunkAbsKey, _chunk);
                }   
                else if(lastCurrentChunks.ContainsKey(_chunkAbsKey))
                {
                    _chunk = lastCurrentChunks[_chunkAbsKey];
                    SetChunkState(_chunk, x, y);

                    lastCurrentChunks.Remove(_chunkAbsKey);
                }
                else
                {
                    _chunk = new Chunk(ChunkIndex, _chunkAbsKey, _chunkWorldKey);
                    chunksDict.Add(_chunkAbsKey, _chunk);
                    SetChunkState(_chunk, x, y);
                    // _currentActiveChunks.Add(_chunkAbsKey, _chunk);
                }

            }
        }
    }

    private void SetChunkState(Chunk _chunk, int _x, int _y)
    {
        if (Math.Abs(_x) <= 5 && Math.Abs(_y) <= 5)
        {
            _chunk.SetActiveChunk();
            return;
        }

        _chunk.SetLazyChunk();
    }

    private void MarkChunksInactive()
    {
        foreach (var chunk in lastCurrentChunks.Values)
        {
            chunk.SetInactiveChunk();
        }

        lastCurrentChunks.Clear();
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