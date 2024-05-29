using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
public class ChunkManager2 : MonoBehaviour
{
    // public static ChunkManager Instance { get; private set; }

    // private const int chunkDiameter = 600;
    // public int ChunkDiameter { get => chunkDiameter; }

    // private IPlayerPositionService playerPositionService;
    // private FloatingOriginSystem floatingOriginSystem;
    // private StarGenerator starGenerator;
    // private Transform cameraTransform;
    // private Transform playerTransform;

    // private Dictionary<Vector2Int, Chunk> chunksDict = new Dictionary<Vector2Int, Chunk>();
    // private Dictionary<Vector2Int, Chunk> lastCurrentChunks = new Dictionary<Vector2Int, Chunk>();
    // private ObjectPool<GameObject> chunkPool;
    // private long chunkIndex = 0;

    // private Dictionary<Vector2Int, Chunk> starChunks = new Dictionary<Vector2Int, Chunk>();
    // private List<Vector2> currentStarPositions = new List<Vector2>();

    // public UnityEvent OnUpdateChunks = new UnityEvent();

    // public IPlayerPositionService PlayerPositionService { get => playerPositionService; }
    // public Dictionary<Vector2Int, Chunk> ChunksDict { get => chunksDict; }
    // public ObjectPool<GameObject> ChunkPool { get => chunkPool; }
    // private long ChunkIndex { get => chunkIndex++; }
    // public List<Vector2> CurrentStarPositions { get => currentStarPositions; }

    // public ChunkManager()
    // {
    //     if (Instance == null)
    //     {
    //         Instance = this;
    //     }
    //     else
    //     {
    //         Destroy(gameObject);
    //     }
    // }

    // private void Awake()
    // {
    //     playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    //     starGenerator = GetComponent<StarGenerator>();
    //     cameraTransform = Camera.main.transform;

    //     playerPositionService = GetComponent<PlayerPositionService>();
    //     floatingOriginSystem = GetComponent<FloatingOriginSystem>();
    // }

    // private void Start()
    // {
    //     chunkPool = new ObjectPool<GameObject>(() => 
    //     {
    //         return new GameObject("Chunk");
    //     }, _chunkObject => 
    //     {
    //         _chunkObject.SetActive(true);
    //     }, _chunkObject => 
    //     {
    //         _chunkObject.SetActive(false);
    //     }, _chunkObject => 
    //     {
    //         Destroy(_chunkObject);
    //     }, false, 150, 200);

    //     // CreateWorldMap();
    //     GetCurrentChunks();
    // }

    // private void Update()
    // {   
    //     if (playerPositionService.GetAbsoluteChunkPosition() != playerPositionService.GetLastAbsoluteChunkPosition())
    //     {
    //         var currentChunks = GetCurrentChunks();
    //         MarkChunksInactive(currentChunks);
    //     }
    // }

    // private void CreateWorldMap()
    // {
    //     const int minStarRange = -5;
    //     const int maxStarRange = 5;

    //     const int minExcludeRange = -6;
    //     const int maxExcludeRange = 6;

    //     var starRimChunks = new List<Vector2Int>();

    //     for (int x = -15; x <= 15; x++)
    //     {
    //         for (int y = -15; y <= 15; y++)
    //         {
    //             var _chunkAbsKey = GetChunkAbsKey(x, y);    //For searching dictionary
    //             var _chunkWorldKey = GetChunkWorldKey(x, y);    //For placing chunk in world

    //             Chunk _chunk;

    //             //If within a box at min and max star range 
    //             if ((x == minStarRange || x == maxStarRange) && (y >= minStarRange && y <= maxStarRange) ||
    //                 (y == minStarRange || y == maxStarRange) && (x >= minStarRange && x <= maxStarRange))
    //             {
    //                 starRimChunks.Add(_chunkAbsKey);
    //             }
    //             else if ((x > minExcludeRange && x < maxExcludeRange && y > minExcludeRange && y < maxExcludeRange))
    //             {
    //                 _chunk = CreateChunk(_chunkAbsKey, preventMakeStar: true);
    //             }
    //             else
    //             {
    //                 _chunk = CreateChunk(_chunkAbsKey);
    //             }
    //         }
    //     }

    //     CreateInitialStarChunk(starRimChunks);
    // }   

    // private void CreateInitialStarChunk(List<Vector2Int> starRimChunks)
    // {
    //     var randomChunk = UnityEngine.Random.Range(0, starRimChunks.Count);
    //     Vector2Int selectedChunk = starRimChunks[randomChunk];

    //     Chunk _starChunk;

    //     foreach (var starChunkPosition in starRimChunks)
    //     {
    //         if (starChunkPosition == selectedChunk)
    //         {
    //             _starChunk = CreateChunk(starChunkPosition, makeStar: true);
    //             continue;
    //         }

    //         _starChunk = CreateChunk(starChunkPosition, preventMakeStar: true);
    //     }
    // }

    // private List<Chunk> GetCurrentChunks()
    // {
    //     List<Chunk> currentChunks = new List<Chunk>();

    //     currentStarPositions.Clear();
        
    //     for (int x = -3; x <= 3; x++)
    //     {
    //         for (int y = -3; y <= 3; y++)
    //         {
    //             var _chunkAbsKey = GetChunkAbsKey(x, y);    //For searching dictionary
    //             var _chunkWorldKey = GetChunkWorldKey(x, y);    //For placing chunk in world

    //             Chunk _chunk;

    //             if (chunksDict.ContainsKey(_chunkAbsKey))
    //             {
    //                 _chunk = chunksDict[_chunkAbsKey];
    //                 SetChunkState(_chunk, _chunkWorldKey, x, y);
    //             }   
    //             else if(lastCurrentChunks.ContainsKey(_chunkAbsKey))
    //             {
    //                 _chunk = lastCurrentChunks[_chunkAbsKey];
    //                 SetChunkState(_chunk, _chunkWorldKey, x, y);

    //                 lastCurrentChunks.Remove(_chunkAbsKey);
    //             }
    //             else
    //             {
    //                 _chunk = CreateChunk(_chunkAbsKey);
    //                 SetChunkState(_chunk, _chunkWorldKey, x, y);
    //             }

    //             if (_chunk.HasStar && !currentStarPositions.Contains(_chunk.StarPosition))
    //             {
    //                 currentStarPositions.Add(_chunk.StarPosition);
    //             }

    //             currentChunks.Add(_chunk);
    //         }
    //     }

    //     OnUpdateChunks.Invoke();

    //     return currentChunks;
    // }

    // private Chunk CreateChunk(Vector2Int _chunkAbsKey, bool makeStar = false, bool preventMakeStar = false)
    // {
    //     Chunk _chunk = new Chunk(ChunkIndex, _chunkAbsKey, makeStar, preventMakeStar);

    //     if (!chunksDict.ContainsKey(_chunkAbsKey))
    //     {
    //         chunksDict.Add(_chunkAbsKey, _chunk);
    //     }

    //     if (_chunk.HasStar && !starChunks.ContainsKey(_chunkAbsKey))
    //     {
    //         starChunks.Add(_chunkAbsKey, _chunk);
    //     }

    //     return _chunk;
    // } 

    // private void SetChunkState(Chunk _chunk, Vector2 _chunkCurrentKey, int _x, int _y)
    // {
    //     if (Math.Abs(_x) <= 3 && Math.Abs(_y) <= 3)
    //     {
    //         _chunk.SetActiveChunk(playerPositionService.GetWorldChunkPosition(), _chunkCurrentKey);
    //         return;
    //     }

    //     _chunk.SetLazyChunk();
    // }

    // private void MarkChunksInactive(List<Chunk> currentChunks)
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
    //         lastCurrentChunks.Add(chunk.ChunkKey, chunk);
    //     }
    // }

    // private Vector2Int GetChunkAbsKey(int x, int y)
    // {
    //     Vector2Int absChunkPos = playerPositionService.GetAbsoluteChunkPosition();

    //     return new Vector2Int(
    //         absChunkPos.x + x,
    //         absChunkPos.y + y
    //     );
    // }

    // private Vector2Int GetChunkWorldKey(int x, int y)
    // {
    //     Vector2Int worldChunkPos = playerPositionService.GetWorldChunkPosition();

    //     return new Vector2Int(
    //         worldChunkPos.x + x,
    //         worldChunkPos.y + y
    //     );
    // }

}
}