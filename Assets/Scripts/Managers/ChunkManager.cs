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

    private HashSet<Vector2Int> starChunks = new HashSet<Vector2Int>();
    private List<Vector2Int> currentStarChunks = new List<Vector2Int>();

    public UnityEvent OnUpdateChunks = new UnityEvent();

    private int chunkDiameter = 1000;
    private uint chunkIndex = 0;

    private uint ChunkIndex { get => chunkIndex++; }
    public int ChunkDiameter { get => chunkDiameter; }

    public Dictionary<Vector2Int, Chunk> ChunksDict { get => chunksDict; }
    public ObjectPool<GameObject> ChunkPool { get => chunkPool; }
    public List<Vector2Int> CurrentStarChunks { get => currentStarChunks; }

    // public ChunkManager()
    // {
        
    // }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

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

        CreateWorldMap();
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

    private void CreateWorldMap()
    {
        const int minStarRange = -3;
        const int maxStarRange = 3;

        const int minExcludeRange = -5;
        const int maxExcludeRange = 5;

        var starRimChunks = new List<Vector2Int>();

        for (int x = -15; x <= 15; x++)
        {
            for (int y = -15; y <= 15; y++)
            {
                var _chunkAbsKey = GetChunkAbsKey(x, y);    //For searching dictionary
                var _chunkWorldKey = GetChunkPosition(x, y);    //For placing chunk in world

                Chunk _chunk;

                //If within a box at min and max star range 
                if ((x == minStarRange || x == maxStarRange) && (y >= minStarRange && y <= maxStarRange) ||
                    (y == minStarRange || y == maxStarRange) && (x >= minStarRange && x <= maxStarRange))
                {
                    starRimChunks.Add(_chunkAbsKey);
                }
                else if ((x > minExcludeRange && x < maxExcludeRange && y > minExcludeRange && y < maxExcludeRange))
                {
                    _chunk = CreateChunk(_chunkAbsKey, preventMakeStar: true);
                }
                else
                {
                    _chunk = CreateChunk(_chunkAbsKey);
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
                _starChunk = CreateChunk(starChunkPosition, makeStar: true);
                continue;
            }

            _starChunk = CreateChunk(starChunkPosition, preventMakeStar: true);
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

        Minimap.Instance.UpdateMinimapMarkers();
    }

    private List<Chunk> currentChunks = new List<Chunk>();
    private void GetCurrentChunks()
    {
        currentChunks = new List<Chunk>();

        for (int x = -5; x <= 5; x++)
        {
            for (int y = -5; y <= 5; y++)
            {
                Vector2Int chunkAbsKey = GetChunkAbsKey(x, y);
                Vector2Int chunkPosition = GetChunkPosition(x, y);

                Chunk currentChunk;

                if (chunksDict.ContainsKey(chunkAbsKey))
                {
                    currentChunk = chunksDict[chunkAbsKey];
                    SetChunkState(currentChunk, chunkPosition, x, y);
                }
                else
                {
                    currentChunk = CreateChunk(chunkAbsKey);
                    SetChunkState(currentChunk, chunkPosition, x, y);
                }

                if (currentChunk.HasStar && !currentStarChunks.Contains(chunkAbsKey))
                {
                    currentStarChunks.Add(chunkAbsKey);
                }

                currentChunks.Add(currentChunk);
            }
        }

        OnUpdateChunks.Invoke();
    }

    private List<Chunk> lastCurrentChunks = new List<Chunk>();
    private void MarkChunksInactive()
    {
        var currentChunksSet = new HashSet<Chunk>(currentChunks);

        foreach (var chunk in lastCurrentChunks)
        {
            if (!currentChunksSet.Contains(chunk))
            {
                if (chunk.HasStar && currentStarChunks.Contains(chunk.ChunkKey))
                {
                    currentStarChunks.Remove(chunk.ChunkKey);
                }

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

        if (_chunk.HasStar && !starChunks.Contains(_chunkAbsKey))
        {
            starChunks.Add(_chunkAbsKey);
        }

        return _chunk;
    } 

    private void SetChunkState(Chunk _chunk, Vector2Int _chunkCurrentKey, int _x, int _y)
    {
        if (Math.Abs(_x) <= 5 && Math.Abs(_y) <= 5)
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