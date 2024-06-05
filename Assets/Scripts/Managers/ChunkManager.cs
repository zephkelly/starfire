using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;

namespace Starfire
{
[RequireComponent(typeof(StarGenerator))]
[RequireComponent(typeof(PlanetGenerator))]
public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    #region Player
    private ShipController playerController;
    private Transform playerTransform;
    private Vector2 playerLastPosition;

    private Vector2D playerAbsolutePosition;
    private Vector2Int playerAbsoluteChunkPosition;
    private Vector2Int playerLastAbsoluteChunkPosition;
    private Vector2Int playerCurrentChunkKey;
    private Vector2Int playerLastCurrentChunkKey;
    #endregion

    #region Camera
    private Camera mainCamera;
    private CameraController cameraController;
    #endregion

    #region ChunkManager Settings
    private ObjectPool<GameObject> chunkPool;
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    private List<Vector2Int> currentChunks = new List<Vector2Int>();
    private List<Vector2Int> lastCurrentChunks = new List<Vector2Int>();

    private HashSet<Vector2Int> starChunks = new HashSet<Vector2Int>();
    private List<Vector2Int> currentStarChunks = new List<Vector2Int>();

    private const int chunkDiameter = 1000;
    [SerializeField] private float floatingOriginLimit = 2500f;
    private uint chunkIndex = 0;
    #endregion

    private NameGenerator nameGenerator = new NameGenerator(); 

    #region Stars
    private StarGenerator starGenerator;
    #endregion

    #region Planets
    private PlanetGenerator planetGenerator;
    #endregion

    public ObjectPool<GameObject> ChunkPool { get => chunkPool; }
    public Dictionary<Vector2Int, Chunk> Chunks { get => chunks; }
    public List<Vector2Int> CurrentStarChunks { get => currentStarChunks; }
    public StarGenerator StarGenerator { get => starGenerator; }
    public PlanetGenerator PlanetGenerator { get => planetGenerator; }
    public NameGenerator NameGenerator { get => nameGenerator; }
    public int ChunkDiameter { get => chunkDiameter; }
    public uint ChunkIndex { get => chunkIndex++; }

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

        starGenerator = GetComponent<StarGenerator>();
        planetGenerator = GetComponent<PlanetGenerator>();

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
        }, false, 200, 300);
    }

    private void Start()
    {
        mainCamera = Camera.main;
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        playerLastPosition = playerTransform.position;
        playerController = playerTransform.GetComponent<ShipController>();
        cameraController = mainCamera.GetComponent<CameraController>();

        CreateWorldMap();
        currentChunks = GetCurrentChunks();
        MarkLastChunksInactive();
        Minimap.Instance.UpdateMinimapMarkers();
    }

    private void Update()
    {
        UpdatePlayerPosition();

        bool resetOrigin = false;

        if (playerTransform.position.magnitude > floatingOriginLimit)
        {
            resetOrigin = ResetFloatingOrigin();
        }
        
        if (playerCurrentChunkKey != playerLastCurrentChunkKey)
        {
            MarkLastChunksInactive();
            currentChunks = GetCurrentChunks();

            if (resetOrigin)
            {
                Minimap.Instance.UpdateMinimapMarkers();
            }
        }

        UpdateLastPositions();
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

                //If within a box at min and max star range 
                if ((x == minStarRange || x == maxStarRange) && (y >= minStarRange && y <= maxStarRange) ||
                    (y == minStarRange || y == maxStarRange) && (x >= minStarRange && x <= maxStarRange))
                {
                    starRimChunks.Add(_chunkAbsKey);
                }
                else if (x > minExcludeRange && x < maxExcludeRange && y > minExcludeRange && y < maxExcludeRange)
                {
                    CreateChunk(_chunkAbsKey, preventMakeStar: true);
                }
                else
                {
                    CreateChunk(_chunkAbsKey);
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

    private List<Vector2Int> GetCurrentChunks()
    {
        List<Vector2Int> currentChunks = new List<Vector2Int>();

        for (int x = -9; x <= 9; x++)
        {
            for (int y = -9; y <= 9; y++)
            {
                Vector2Int chunkAbsKey = GetChunkAbsKey(x, y);
                Vector2Int chunkPosition = GetChunkPosition(x, y);

                Chunk currentChunk;

                if (chunks.ContainsKey(chunkAbsKey))
                {
                    currentChunk = chunks[chunkAbsKey];
                }
                else
                {
                    currentChunk = CreateChunk(chunkAbsKey);
                }

                SetChunkState(currentChunk, chunkPosition, x, y);

                if (currentChunk.HasStar && !currentStarChunks.Contains(chunkAbsKey))
                {
                    currentStarChunks.Add(chunkAbsKey);
                }

                currentChunks.Add(chunkAbsKey);
            }
        }

        return currentChunks;
    }

    private void MarkLastChunksInactive()
    {
        var hashChunkSet = new HashSet<Vector2Int>(currentChunks);

        foreach (var chunkKey in lastCurrentChunks)
        {
            if (hashChunkSet.Contains(chunkKey)) continue;

            chunks[chunkKey].SetInactiveChunk();

            if (chunks[chunkKey].HasStar && currentStarChunks.Contains(chunkKey))
            {
                currentStarChunks.Remove(chunkKey);
            }
        }

        lastCurrentChunks.Clear();
        lastCurrentChunks = new List<Vector2Int>(currentChunks);
    }

    
    private bool ResetFloatingOrigin()
    {
        // Get player distance from current chunk center
        Vector2Int playerChunk = playerAbsoluteChunkPosition;
        Vector2 chunkCenter = Chunks[playerChunk].ChunkObject.transform.position;
        Vector2 playerPosition = playerTransform.position;
        Vector2 distanceFromChunkCenter = playerPosition - chunkCenter;

        // Calculate offset
        Vector2 offset = -(Vector2)playerTransform.position;
        offset += distanceFromChunkCenter;

        // Transport player + camera
        playerController.Transport(offset);
        cameraController.Transport(offset);

        ClearLastPositions();
        UpdatePlayerPosition();

        playerCurrentChunkKey = ChunkUtils.GetChunkPosition((Vector2)playerTransform.position, chunkDiameter);

        // Update minimap markers
        Minimap.Instance.UpdateMinimapMarkers();

        return true;
    }

    private Chunk CreateChunk(Vector2Int _chunkAbsKey, bool makeStar = false, bool preventMakeStar = false)
    {
        Chunk _chunk = new Chunk(ChunkIndex, _chunkAbsKey, makeStar, preventMakeStar);

        if (!chunks.ContainsKey(_chunkAbsKey))
        {
            chunks.Add(_chunkAbsKey, _chunk);
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

    private Vector2Int GetChunkAbsKey(int x, int y)
    {
        Vector2Int absChunkPos = playerAbsoluteChunkPosition;

        return new Vector2Int(
            absChunkPos.x + x,
            absChunkPos.y + y
        );
    }

    private Vector2Int GetChunkPosition(int x, int y)
    {
        Vector2Int worldChunkPos = ChunkUtils.GetChunkPosition(playerTransform.position, ChunkDiameter);

        return new Vector2Int(
            worldChunkPos.x + x,
            worldChunkPos.y + y
        );
    }

    private void SetChunkState(Chunk _chunk, Vector2Int _chunkCurrentKey, int _x, int _y)
    {
        if (Math.Abs(_x) <= 3 && Math.Abs(_y) <= 3)
        {
            _chunk.SetActiveChunk(_chunkCurrentKey);
            return;
        }

        _chunk.SetLazyChunk(_chunkCurrentKey);
    }

    private void UpdatePlayerPosition()
    {
        playerAbsolutePosition += UpdatePlayerAbsolutePosition();

        playerAbsoluteChunkPosition = ChunkUtils.GetChunkPosition(playerAbsolutePosition, chunkDiameter);
        playerCurrentChunkKey = ChunkUtils.GetChunkPosition(playerTransform.position, chunkDiameter);
    }

    private Vector2D UpdatePlayerAbsolutePosition()
    {
        Vector2D offsetAmount = new Vector2D(
            playerTransform.position.x - playerLastPosition.x,
            playerTransform.position.y - playerLastPosition.y
        );

        playerLastPosition = playerTransform.position;
        return offsetAmount;
    }

    private void UpdateLastPositions()
    {
        playerLastPosition = playerTransform.position;
        playerLastAbsoluteChunkPosition = playerAbsoluteChunkPosition;
        playerLastCurrentChunkKey = playerCurrentChunkKey;
    }

    private void ClearLastPositions()
    {
        playerLastPosition = playerTransform.position;
    }
}
}