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
    private ObjectPool<GameObject> chunkPool;

    private Dictionary<Vector2Int, Chunk> starChunks = new Dictionary<Vector2Int, Chunk>();
    private List<Vector2> currentStarPositions = new List<Vector2>();

    public UnityEvent OnUpdateChunks = new UnityEvent();

    private int chunkDiameter = 1000;
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
        MarkChunksInactive();
    }

    private void Update()
    {
        if (playerPositionService.GetAbsoluteChunkPosition() != playerPositionService.GetLastAbsoluteChunkPosition())
        {
            GetCurrentChunks();
            MarkChunksInactive();
        }
    }

    public void Transport(Vector2 offset)
    {
        foreach (var chunk in currentChunks)
        {
            if (chunk.ChunkObject != null)
            {
                chunk.ChunkObject.transform.position += (Vector3)offset;
            }
        }
    }

    private List<Chunk> currentChunks = new List<Chunk>();
    private void GetCurrentChunks()
    {
        currentChunks = new List<Chunk>();

        for (int x = -5; x <= 5; x++)
        {
            for (int y = -5; y <= 5; y++)
            {
                Vector2Int chunkKey = GetChunkAbsKey(x, y);
                Vector2Int chunkPosition = GetChunkPosition(x, y);

                Chunk currentChunk;

                if (chunksDict.ContainsKey(chunkKey))
                {
                    currentChunk = chunksDict[chunkKey];
                    SetChunkState(currentChunk, chunkPosition, x, y);
                }
                else
                {
                    currentChunk = CreateChunk(chunkKey);
                    SetChunkState(currentChunk, chunkPosition, x, y);
                }

                currentChunks.Add(currentChunk);
            }
        }

        OnUpdateChunks.Invoke();
    }

    private List<Chunk> lastCurrentChunks = new List<Chunk>();
    private void MarkChunksInactive(bool flush = false)
    {
        var currentChunksSet = new HashSet<Chunk>(currentChunks);

        if (flush)
        {
            // foreach (var chunk in lastCurrentChunks)
            // {
            //     chunk.SetInactiveChunk();
            // }

            // lastCurrentChunks.Clear();

            // lastCurrentChunks = new List<Chunk>(currentChunks);
            return;
        }


        foreach (var chunk in lastCurrentChunks)
        {
            if (!currentChunksSet.Contains(chunk))
            {
                chunk.SetInactiveChunk();
            }
        }

        lastCurrentChunks = new List<Chunk>(currentChunks);
    }

    private Chunk CreateChunk(Vector2Int _chunkAbsKey, bool makeStar = false, bool preventMakeStar = false)
    {
        Chunk _chunk = new Chunk(ChunkIndex, _chunkAbsKey, makeStar, preventMakeStar);

        if (!chunksDict.ContainsKey(_chunkAbsKey))
        {
            chunksDict.Add(_chunkAbsKey, _chunk);
        }
        else
        {
            Debug.LogWarning("Chunk already exists in dictionary.");
        }

        if (_chunk.HasStar && !starChunks.ContainsKey(_chunkAbsKey))
        {
            starChunks.Add(_chunkAbsKey, _chunk);
        }

        return _chunk;
    } 

    private void SetChunkState(Chunk _chunk, Vector2Int _chunkCurrentKey, int _x, int _y)
    {
        if (Math.Abs(_x) <= 1 && Math.Abs(_y) <= 1)
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