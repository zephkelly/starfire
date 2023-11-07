using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Starfire
{
  public interface IStarGenerator
  {
    ObjectPool<GameObject> StarPool { get; }
    bool ShouldSpawnStar(Vector2Int _position);
    Vector2 GetStarPosition(int chunkDiameter, float divisionFactor = 3f);
  }

  [RequireComponent(typeof(ChunkManager))]
  public class StarGenerator : MonoBehaviour, IStarGenerator
  {
    public static StarGenerator Instance { get; private set; }

    private ChunkManager chunkManager;

    private GameObject starPrefab;
    private ObjectPool<GameObject> starPool;

    [SerializeField] private float noiseScale = 0.1f; // Smaller values make smoother noise.
    [SerializeField] private float starSpawnThreshold = 0.7f; // Threshold for spawning a star.

    public ObjectPool<GameObject> StarPool { get => starPool; }

    private void Awake()
    {
      if (Instance == null) {
        Instance = this;
      } else {
        Destroy(gameObject);
      }

      chunkManager = GetComponent<ChunkManager>();
      starPrefab = Resources.Load<GameObject>("Prefabs/Stars/Star");
    }

    private void Start()
    {
      starPool = new ObjectPool<GameObject>(() => 
      {
        return Instantiate(starPrefab);
      }, _starObject => 
      {
        _starObject.SetActive(false);
      }, _starObject => 
      {
        _starObject.SetActive(false);
        _starObject.transform.position = Vector3.zero;
      }, _starObject => 
      {
        Destroy(_starObject);
      }, false, 5, 10);
    }

    public bool ShouldSpawnStar(Vector2Int chunkKey)
    {
      float perlinValue = Mathf.PerlinNoise(chunkKey.x * noiseScale, chunkKey.y * noiseScale);

      if (perlinValue > starSpawnThreshold)
      {
        if (Random.Range(0, 100) > 6) return false;

        var searchDistance = 5;
        for (int x = -searchDistance; x <= searchDistance; x++)
        {
          for (int y = -searchDistance; y <= searchDistance; y++)
          {
            Vector2Int searchChunkKey = new Vector2Int(
              chunkKey.x + x,
              chunkKey.y + y
            );

            if (chunkManager.ChunksDict.ContainsKey(searchChunkKey) && chunkManager.ChunksDict[searchChunkKey].HasStar)
            {
              return false;
            }
          }
        }

        return true;
      }

      return false;
    }

    public Vector2 GetStarPosition(int chunkDiameter, float divisionFactor = 3f)
    {
      return new Vector2(
        Random.Range(-chunkDiameter / divisionFactor, chunkDiameter / divisionFactor),
        Random.Range(-chunkDiameter / divisionFactor, chunkDiameter / divisionFactor)
      );
    }
  }
}