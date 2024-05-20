using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Starfire
{
  public interface IStarGenerator
  {
    ObjectPool<GameObject> StarPool { get; }
    bool ShouldSpawnStar(Vector2Int _position, bool _makeStar = false, bool preventMakeStar = false);
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
    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 1f; // Chance of spawning a star.

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

      starPool = new ObjectPool<GameObject>(() => 
      {
        return Instantiate(starPrefab);
      }, _starObject => 
      {
        _starObject.SetActive(true);
      }, _starObject => 
      {
        _starObject.SetActive(false);
      }, _starObject => 
      {
        Destroy(_starObject);
      }, false, 5, 10);
    }

    public bool ShouldSpawnStar(Vector2Int chunkKey, bool makeStar = false, bool preventMakeStar = false)
    {
        if (preventMakeStar == true) return false;
        if(makeStar == true) return true;

        float perlinValue = Mathf.PerlinNoise(chunkKey.x * noiseScale, chunkKey.y * noiseScale);

        if (perlinValue > starSpawnThreshold)
        {
            if (Random.Range(0f, 100f) > spawnChance) return false;

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