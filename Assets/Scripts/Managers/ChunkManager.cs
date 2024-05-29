using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace Starfire
{
[RequireComponent(typeof(StarGenerator))]
[RequireComponent(typeof(FloatingOriginSystem))]
[RequireComponent(typeof(PlayerPositionService))]
public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    private IPlayerPositionService playerPositionService;
    private FloatingOriginSystem floatingOriginSystem;
    private StarGenerator starGenerator;

    private Transform playerTransform;

    private Dictionary<Vector2Int, Chunk> chunksDict = new Dictionary<Vector2Int, Chunk>();
    // private Dictionary<Vector2Int, Chunk> currentChunks = new Dictionary<Vector2Int, Chunk>();
    // private Dictionary<Vector2Int, Chunk> lastCurrentChunks = new Dictionary<Vector2Int, Chunk>();
    private ObjectPool<GameObject> chunkPool;

    private Dictionary<Vector2Int, Chunk> starChunks = new Dictionary<Vector2Int, Chunk>();
    private List<Vector2> currentStarPositions = new List<Vector2>();

    public UnityEvent OnUpdateChunks = new UnityEvent();

    private int chunkDiameter = 600;
    private uint chunkIndex = 0;

    private uint ChunkIndex { get => chunkIndex++; }
    public int ChunkDiameter { get => chunkDiameter; }

    public Dictionary<Vector2Int, Chunk> ChunksDict { get => chunksDict; }
    public ObjectPool<GameObject> ChunkPool { get => chunkPool; }
    public List<Vector2> CurrentStarPositions { get => currentStarPositions; }

    public ChunkManager()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        starGenerator = GetComponent<StarGenerator>();
        playerPositionService = GetComponent<PlayerPositionService>();
        floatingOriginSystem = GetComponent<FloatingOriginSystem>();
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

        GetCurrentChunks();
    }

    private void Update()
    {
        if (playerPositionService.GetAbsoluteChunkPosition() != playerPositionService.GetLastAbsoluteChunkPosition())
        {
            GetCurrentChunks();
            // MarkChunksInactive();
        }
    }

    private void GetCurrentChunks()
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                var chunkKey = GetChunkAbsKey(x, y);
                var chunkPosition = GetChunkPosition(x, y);

                Chunk currentChunk;

                if (chunksDict.ContainsKey(chunkKey))
                {
                    currentChunk = chunksDict[chunkKey];
                    SetChunkState(currentChunk, chunkPosition, x, y);
                }
                // else if (lastCurrentChunks.ContainsKey(chunkKey))
                // {
                //     currentChunk = lastCurrentChunks[chunkKey];
                //     SetChunkState(currentChunk, chunkPosition, x, y);
                // }
                else
                {
                    currentChunk = CreateChunk(chunkKey);
                    SetChunkState(currentChunk, chunkPosition, x, y);
                }

                // if (!currentChunks.ContainsKey(chunkKey))
                // {
                //     currentChunks.Add(chunkKey, currentChunk);
                // }
            }
        }

        OnUpdateChunks.Invoke();
        // return currentChunks;
    }

    // private void MarkChunksInactive()
    // {
    //     foreach (var chunk in lastCurrentChunks.Values)
    //     {
    //         if (chunk.HasStar && currentStarPositions.Contains(chunk.StarPosition))
    //         {
    //             currentStarPositions.Remove(chunk.StarPosition);
    //         }

    //         chunk.SetInactiveChunk();
    //     }

    //     lastCurrentChunks.Clear();

    //     foreach (var chunk in currentChunks)
    //     {
    //         lastCurrentChunks.Add(chunk.Key, chunk.Value);
    //     }

    //     currentChunks.Clear();
    // }

    private Chunk CreateChunk(Vector2Int _chunkAbsKey, bool makeStar = false, bool preventMakeStar = false)
    {
        Chunk _chunk = new Chunk(ChunkIndex, _chunkAbsKey, makeStar, preventMakeStar);

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

    private void SetChunkState(Chunk _chunk, Vector2 _chunkCurrentKey, int _x, int _y)
    {
        if (Math.Abs(_x) <= 3 && Math.Abs(_y) <= 3)
        {
            _chunk.SetActiveChunk(playerPositionService.GetWorldChunkPosition(), _chunkCurrentKey);
            return;
        }

        _chunk.SetLazyChunk();
    }

    private Vector2Int GetChunkAbsKey(int x, int y)
    {
        Vector2Int absChunkPos = playerPositionService.GetAbsoluteChunkPosition();

        return new Vector2Int(
            absChunkPos.x + x,
            absChunkPos.y + y
        );
    }

    private Vector2Int GetChunkPosition(int x, int y)
    {
        Vector2Int worldChunkPos = playerPositionService.GetWorldChunkPosition();

        return new Vector2Int(
            worldChunkPos.x + x,
            worldChunkPos.y + y
        );
    }
}
}