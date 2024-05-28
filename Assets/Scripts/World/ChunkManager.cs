using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace Starfire
{
  [RequireComponent(typeof(StarGenerator))]
  public class ChunkManager : MonoBehaviour
  {
    public static ChunkManager Instance { get; private set; }
    private StarGenerator starGenerator;

    const int chunkDiameter = 600;

    private Transform cameraTransform;
    private Transform entityTransform;

    private Vector2 entityLastPosition;
    private Vector2Int entityWorldChunkPosition;
    private Vector2Int entityLastWorldChunkPosition;

    private Vector2D entityAbsolutePosition;
    private Vector2Int entityAbsoluteChunkPosition;
    private Vector2Int entityLastAbsoluteChunkPosition;

    private Dictionary<Vector2Int, Chunk> chunksDict = new Dictionary<Vector2Int, Chunk>();
    private Dictionary<Vector2Int, Chunk> lastCurrentChunks = new Dictionary<Vector2Int, Chunk>();
    private ObjectPool<GameObject> chunkPool;
    private long chunkIndex = 0;

    private Dictionary<Vector2Int, Chunk> starChunks = new Dictionary<Vector2Int, Chunk>();
    private List<Vector2> currentStarPositions = new List<Vector2>();
    public List<Vector2> CurrentStarPositions { get => currentStarPositions; }

    public UnityEvent OnUpdateChunks = new UnityEvent();

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

        CreateWorldMap();
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

    private void CreateWorldMap()
    {
        const int minStarRange = -5;
        const int maxStarRange = 5;

        const int minExcludeRange = -6;
        const int maxExcludeRange = 6;

        var starRimChunks = new List<Vector2Int>();

        for (int x = -15; x <= 15; x++)
        {
            for (int y = -15; y <= 15; y++)
            {
                var _chunkAbsKey = GetChunkAbsKey(x, y);    //For searching dictionary
                var _chunkWorldKey = GetChunkWorldKey(x, y);    //For placing chunk in world

                Chunk _chunk;

                //If within a box at min and max star range 
                if ((x == minStarRange || x == maxStarRange) && (y >= minStarRange && y <= maxStarRange) ||
                    (y == minStarRange || y == maxStarRange) && (x >= minStarRange && x <= maxStarRange))
                {
                    starRimChunks.Add(_chunkAbsKey);
                }
                else if ((x > minExcludeRange && x < maxExcludeRange && y > minExcludeRange && y < maxExcludeRange))
                {
                    _chunk = CreateChunk(_chunkAbsKey, _chunkWorldKey, preventMakeStar: true);
                }
                else
                {
                    _chunk = CreateChunk(_chunkAbsKey, _chunkWorldKey);
                }
            }
        }

        CreateInitialStarChunk(starRimChunks);
    }   

    private void CreateInitialStarChunk(List<Vector2Int> starRimChunks)
    {
        var randomChunk = UnityEngine.Random.Range(0, starRimChunks.Count);
        Vector2Int selectedChunk = starRimChunks[randomChunk];

        Chunk _starChunk;

        foreach (var starChunkPosition in starRimChunks)
        {
            if (starChunkPosition == selectedChunk)
            {
                _starChunk = CreateChunk(starChunkPosition, starChunkPosition, makeStar: true);
                continue;
            }

            _starChunk = CreateChunk(starChunkPosition, starChunkPosition, preventMakeStar: true);
        }
    }

    private void GetCurrentChunks()
    {
        currentStarPositions.Clear();
        
        for (int x = -8; x <= 8; x++)
        {
            for (int y = -8; y <= 8; y++)
            {
                var _chunkAbsKey = GetChunkAbsKey(x, y);    //For searching dictionary
                var _chunkWorldKey = GetChunkWorldKey(x, y);    //For placing chunk in world

                Chunk _chunk;

                if (chunksDict.ContainsKey(_chunkAbsKey))
                {
                    _chunk = chunksDict[_chunkAbsKey];
                    SetChunkState(_chunk, x, y);
                }   
                else if(lastCurrentChunks.ContainsKey(_chunkAbsKey))
                {
                    _chunk = lastCurrentChunks[_chunkAbsKey];
                    SetChunkState(_chunk, x, y);

                    lastCurrentChunks.Remove(_chunkAbsKey);
                }
                else
                {
                    _chunk = CreateChunk(_chunkAbsKey, _chunkWorldKey);
                    SetChunkState(_chunk, x, y);
                }

                if (_chunk.HasStar && !currentStarPositions.Contains(_chunk.StarPosition))
                {
                    currentStarPositions.Add(_chunk.StarPosition);
                }
            }
        }

        OnUpdateChunks.Invoke();
    }

    private Chunk CreateChunk(Vector2Int _chunkAbsKey, Vector2Int _chunkWorldKey, bool makeStar = false, bool preventMakeStar = false)
    {
        Chunk _chunk = new Chunk(ChunkIndex, _chunkAbsKey, _chunkWorldKey, makeStar, preventMakeStar);

        if (!chunksDict.ContainsKey(_chunkAbsKey))
        {
            chunksDict.Add(_chunkAbsKey, _chunk);
        }

        if (_chunk.HasStar && !starChunks.ContainsKey(_chunkAbsKey))
        {
            starChunks.Add(_chunkAbsKey, _chunk);
        }

        return _chunk;
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
            if (chunk.HasStar && currentStarPositions.Contains(chunk.StarPosition))
            {
                currentStarPositions.Remove(chunk.StarPosition);
            }

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
      entityAbsoluteChunkPosition = ChunkUtils.GetChunkPosition(entityAbsolutePosition, chunkDiameter);
      entityWorldChunkPosition = ChunkUtils.GetChunkPosition(entityTransform.position, chunkDiameter);
    }

    private void SetLastPositions()
    {
      entityLastPosition = entityTransform.position;
      entityLastAbsoluteChunkPosition = entityAbsoluteChunkPosition;
      entityLastWorldChunkPosition = entityWorldChunkPosition;
    }
  }
}